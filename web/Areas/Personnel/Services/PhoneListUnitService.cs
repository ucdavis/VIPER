using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Services
{
    public class PhoneListUnitService
    {
        private readonly PhonesDbContext _context;
        private readonly IUserHelper _userHelper;
        private readonly PhonePermissionsService _phonePermissionsService;
        public PhoneListUnitService(
            PhonesDbContext context,
            IUserHelper userHelper,
            PhonePermissionsService phonePermissionsService
        )
        {
            _context = context;
            _userHelper = userHelper;
            _phonePermissionsService = phonePermissionsService;
        }

        /// <summary>
        /// Direct numbers are visible to the maintainers of a list and to the people on the list
        /// itself; everyone else with SVMSecure sees them blanked.
        /// </summary>
        public async Task<bool> CanViewDirectPhone(PhoneList list, CancellationToken ct = default)
        {
            var userIam = _userHelper.GetCurrentUser()?.IamId;
            if (string.IsNullOrWhiteSpace(userIam))
            {
                return false;
            }
            if (_phonePermissionsService.CanMaintainList(list))
            {
                return true;
            }
            return await _context.PhoneListUnitPerson
                .AsNoTracking()
                .AnyAsync(t => t.IsActive && t.PersonIam == userIam && t.PhoneListUnit.PhoneListId == list.PhoneListId, ct);
        }

        /// <summary>
        /// Confirms a unit belongs to the list the request was routed through, so a caller who
        /// maintains one list cannot reach the units of another list by passing its unit id.
        /// </summary>
        private async Task VerifyUnitInList(int listId, int unitId, CancellationToken ct)
        {
            var unitInList = await _context.PhoneListUnit
                .AsNoTracking()
                .AnyAsync(t => t.PhoneListUnitId == unitId && t.PhoneListId == listId, ct);
            if (!unitInList)
            {
                throw new InvalidOperationException("Unit not found in this phone list");
            }
        }

        /// <summary>
        /// Confirms the person exists in users.Person. PhonePerson requires that relationship, so
        /// every read projection inner joins to it: a row stored against an unknown IAM ID is
        /// invisible to the list and cannot be edited or removed through it. The person pickers
        /// only offer real people, so this guards against a request that did not come from them.
        /// </summary>
        private async Task VerifyPersonExists(string iamId, CancellationToken ct)
        {
            var personExists = await _context.ViperPerson
                .AsNoTracking()
                .AnyAsync(t => t.IamId == iamId, ct);
            if (!personExists)
            {
                throw new InvalidOperationException("The selected employee could not be found.");
            }
        }

        /// <summary>
        /// Loads an active unit-person row, confirming it belongs to the list the request was
        /// routed through. Returns the row so callers avoid a second lookup.
        /// </summary>
        private async Task<PhoneListUnitPerson> GetUnitPersonInList(int listId, int unitPersonId, CancellationToken ct)
        {
            var unitPerson = await _context.PhoneListUnitPerson
                .FirstOrDefaultAsync(
                    t => t.PhoneListUnitPersonId == unitPersonId && t.PhoneListUnit.PhoneListId == listId && t.IsActive,
                    ct);
            if (unitPerson == null)
            {
                throw new InvalidOperationException("That record has already been removed.");
            }
            return unitPerson;
        }

        /// <summary>
        /// Get all units, and the people in them, for a phone list.
        /// </summary>
        public async Task<List<PhoneListUnit>> GetPhoneListUnits(PhoneList list, CancellationToken ct = default)
        {
            // Only users in a phone list may see direct numbers for that list.
            bool isInternal = await CanViewDirectPhone(list, ct);
            int listId = list.PhoneListId;
            var allUnits = await _context.PhoneListUnit
                .AsNoTracking()
                .Where(t => t.PhoneListId == listId)
                .Select(t => new PhoneListUnit
                {
                    PhoneListUnitId = t.PhoneListUnitId,
                    PhoneListId = t.PhoneListId,
                    Name = t.Name,
                    SortOrder = t.SortOrder,
                    PhoneListUnitPersons = t.PhoneListUnitPersons
                    .Where(p => p.IsActive && p.Person.ViperPerson != null)
                    .Select(p => new PhoneListUnitPerson
                    {
                        PhoneListUnitPersonId = p.PhoneListUnitPersonId,
                        PhoneListUnitId = p.PhoneListUnitId,
                        PersonIam = p.PersonIam,
                        ListFirst = p.ListFirst,
                        IsActive = p.IsActive,
                        ModifiedDate = p.ModifiedDate,
                        ModifiedBy = p.ModifiedBy,
                        Person = new PhonePerson
                        {
                            PersonIam = p.Person.PersonIam,
                            Phone = p.Person.Phone,
                            DirectPhone = isInternal ? p.Person.DirectPhone : "",
                            Office = p.Person.Office,
                            ModifiedDate = p.Person.ModifiedDate,
                            ModifiedBy = p.Person.ModifiedBy,
                            ViperPerson = p.Person.ViperPerson,
                            ViperModPerson = p.Person.ViperModPerson,
                        },
                        ViperModPerson = p.ViperModPerson
                    })
                    .OrderByDescending(p => p.ListFirst)
                    .ThenBy(p => p.Person.ViperPerson!.LastName)
                    .ThenBy(p => p.Person.ViperPerson!.FirstName)
                    .ToList()
                })
                .OrderBy(t => t.SortOrder == null)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            // A small number of current employees have multiple rows in users.Person,
            // so the joins fan out the results in a way that results in multiple rows for
            // these users. Therefore, we need to deduplicate these rows
            // in the Users table. Due to nested selects, this must be done client-side
            // instead of server-side.
            foreach (var unit in allUnits)
            {
                unit.PhoneListUnitPersons = [.. unit.PhoneListUnitPersons.DistinctBy(p => p.Person.PersonIam)];
            }

            return allUnits;
        }

        /// <summary>
        /// Gets the most recent date that a UnitPerson in this list was modified.
        /// </summary>
        public async Task<DateTime?> GetUnitPersonModifiedDate(int listId, CancellationToken ct = default)
        {
            var results = _context.PhoneListUnitPerson
                .AsNoTracking()
                .Where(t => t.ModifiedDate != null && t.PhoneListUnit.PhoneListId == listId)
                .OrderByDescending(t => t.ModifiedDate);
            var lastModifiedRecord = await results.FirstOrDefaultAsync(ct);
            return lastModifiedRecord?.ModifiedDate;
        }

        /// <summary>
        /// Updates a PhonePerson if they exist, or adds them otherwise.
        /// This data is shared across other lists, and so changes here will impact those
        /// indirectly. Phone lists maintainers change office (not shared with SVM),
        /// (office) phone (shared with SVM), and direct phone (not shared with SVM)
        /// in this table.
        /// </summary>
        private async Task AddOrUpdatePhonePerson(
            PhoneListUnitDataRequest request,
            string? userIam,
            DateTime updateTimestamp,
            CancellationToken ct = default
        )
        {
            var modifiedPerson = await _context.PhonePerson.FindAsync(new object?[] { request.EmployeeIam.Trim() }, ct);
            if (modifiedPerson == null)
            {
                modifiedPerson = new PhonePerson
                {
                    PersonIam = request.EmployeeIam.Trim(),
                    Phone = request.Phone.Trim(),
                    DirectPhone = request.DirectPhone.Trim(),
                    Office = request.Office.Trim(),
                    ModifiedDate = updateTimestamp,
                    ModifiedBy = userIam,
                };
                await _context.PhonePerson.AddAsync(modifiedPerson, ct);
            }
            else
            {
                modifiedPerson.Phone = request.Phone.Trim();
                modifiedPerson.DirectPhone = request.DirectPhone.Trim();
                modifiedPerson.Office = request.Office.Trim();
                modifiedPerson.ModifiedBy = userIam;
                modifiedPerson.ModifiedDate = updateTimestamp;
            }
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Updates the ListFirst column for this list's UnitPersons, as only 1
        /// may have this flag at one time.
        /// </summary>
        private async Task UpdateListFirst(int unitId, CancellationToken ct = default)
        {
            var oldFirstPersons = await _context.PhoneListUnitPerson
                .Where(t => t.ListFirst && t.IsActive && t.PhoneListUnitId == unitId)
                .ToListAsync(ct);
            foreach (var oldFirstPerson in oldFirstPersons)
            {
                // Don't update the Modified By or Date for these records,
                // since this may result in confusing front end information.
                oldFirstPerson.ListFirst = false;
            }
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Adds data about a UnitPerson, which also updates or creates a PhonePerson.
        /// </summary>
        public async Task AddUnitPersonData(int listId, PhoneListUnitDataRequest request, CancellationToken ct = default)
        {
            await VerifyUnitInList(listId, request.UnitId, ct);
            await VerifyPersonExists(request.EmployeeIam.Trim(), ct);
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var userIam = _userHelper.GetCurrentUser()?.IamId;
            var updateTimestamp = DateTime.Now;
            await AddOrUpdatePhonePerson(request, userIam, updateTimestamp, ct);

            if (request.ListFirst)
            {
                await UpdateListFirst(request.UnitId, ct);
            }
            var modifiedUnitPerson = await _context.PhoneListUnitPerson
                .Where(p => p.IsActive && p.PhoneListUnitId == request.UnitId && p.PersonIam == request.EmployeeIam.Trim())
                .FirstOrDefaultAsync(ct);
            if (modifiedUnitPerson == null)
            {
                var newPhoneListPerson = new PhoneListUnitPerson
                {
                    PhoneListUnitId = request.UnitId,
                    PersonIam = request.EmployeeIam.Trim(),
                    ListFirst = request.ListFirst,
                    IsActive = true,
                    ModifiedBy = userIam,
                    ModifiedDate = updateTimestamp
                };
                await _context.PhoneListUnitPerson.AddAsync(newPhoneListPerson, ct);
            }
            else
            {
                modifiedUnitPerson.PersonIam = request.EmployeeIam.Trim();
                modifiedUnitPerson.ListFirst = request.ListFirst;
                modifiedUnitPerson.ModifiedBy = userIam;
                modifiedUnitPerson.ModifiedDate = updateTimestamp;
            }
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        /// <summary>
        /// Updates data about a UnitPerson, which also updates the PhonePerson.
        /// </summary>
        public async Task UpdateUnitPersonData(int listId, int unitPersonId, PhoneListUnitDataRequest request, CancellationToken ct = default)
        {
            var modifiedPhoneListPerson = await GetUnitPersonInList(listId, unitPersonId, ct);
            await VerifyPersonExists(request.EmployeeIam.Trim(), ct);
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var userIam = _userHelper.GetCurrentUser()?.IamId;
            var updateTimestamp = DateTime.Now;

            await AddOrUpdatePhonePerson(request, userIam, updateTimestamp, ct);
            if (request.ListFirst)
            {
                // Clear the flag on the unit the record actually lives in, not the one the
                // request claims, so a mismatched UnitId cannot unset the entry of another unit.
                await UpdateListFirst(modifiedPhoneListPerson.PhoneListUnitId, ct);
            }
            modifiedPhoneListPerson.ListFirst = request.ListFirst;
            modifiedPhoneListPerson.ModifiedBy = userIam;
            modifiedPhoneListPerson.ModifiedDate = updateTimestamp;
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        /// <summary>
        /// Delete data about a UnitPerson, though not the corresponding PhonePerson.
        /// </summary>
        public async Task DeleteUnitPersonData(int listId, int unitPersonId, CancellationToken ct = default)
        {
            var personToDelete = await GetUnitPersonInList(listId, unitPersonId, ct);
            personToDelete.ModifiedBy = _userHelper.GetCurrentUser()?.IamId;
            personToDelete.ModifiedDate = DateTime.Now;
            personToDelete.IsActive = false;
            await _context.SaveChangesAsync(ct);
        }
    }
}
