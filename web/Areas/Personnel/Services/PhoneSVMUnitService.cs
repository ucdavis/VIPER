using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Services
{
    public class PhoneSVMUnitService
    {
        private readonly PhonesDbContext _context;
        private readonly IUserHelper _userHelper;
        public PhoneSVMUnitService(PhonesDbContext context, IUserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        /// <summary>
        /// Get every Unit on the SVM phone list, with the people in each.
        ///
        /// Returned in one call rather than per section: the page renders all sections at once,
        /// so fetching per section made the load cost scale with the number of sections for data
        /// that is always wanted together. Callers group by SectionId.
        /// </summary>
        public async Task<List<SVMUnit>> GetSVMUnits(CancellationToken ct = default)
        {
            // Do not return inactive SVMUnitPerson records.
            // These are present for a more accurate modification date value.
            var sectionUnits = await _context.SVMUnit
                .AsNoTracking()
                .Select(t => new SVMUnit
                {
                    UnitId = t.UnitId,
                    SectionId = t.SectionId,
                    Name = t.Name,
                    Abbrv = t.Abbrv,
                    SortOrder = t.SortOrder,
                    Fax = t.Fax,
                    ModifiedBy = t.ModifiedBy,
                    ModifiedDate = t.ModifiedDate,
                    UnitPersons = t.UnitPersons
                    .Where(up => up.IsActive)
                    .Select(up => new SVMUnitPerson
                    {
                        UnitPersonId = up.UnitPersonId,
                        UnitId = up.UnitId,
                        PersonIam = up.PersonIam,
                        Office = up.Office,
                        PosType = up.PosType,
                        Interim = up.Interim,
                        ModifiedDate = up.ModifiedDate,
                        ModifiedBy = up.ModifiedBy,
                        IsActive = up.IsActive,
                        Person = new PhonePerson
                        {
                            PersonIam = up.Person.PersonIam,
                            Phone = up.Person.Phone,
                            // This data is never needed for SVM lists.
                            DirectPhone = "",
                            Office = up.Person.Office,
                            ModifiedDate = up.Person.ModifiedDate,
                            ModifiedBy = up.Person.ModifiedBy,
                            ViperPerson = up.Person.ViperPerson,
                            ViperModPerson = up.Person.ViperModPerson
                        },
                        ViperModPerson = up.ViperModPerson
                    })
                    .ToList(),
                    ViperModPerson = t.ViperModPerson
                })
                .OrderBy(t => t.SectionId)
                .ThenBy(t => t.SortOrder == null)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            // Need to deduplicate rows caused by multiple records for the same person
            // in the Users table. Due to nested selects, this must be done client-side
            // instead of server-side.
            foreach (var unit in sectionUnits)
            {
                unit.UnitPersons = [.. unit.UnitPersons.DistinctBy(p => p.Person.PersonIam)];
            }

            return sectionUnits;
        }

        /// <summary>
        /// Gets the most recent date an SVMUnitPerson record was modified.
        /// </summary>
        public async Task<DateTime?> GetSVMUnitPersonModifiedDate(CancellationToken ct = default)
        {
            var results = _context.SVMUnitPerson
                .AsNoTracking()
                .Where(t => t.ModifiedDate != null)
                .OrderByDescending(t => t.ModifiedDate);
            var lastModifiedRecord = await results.FirstOrDefaultAsync(ct);
            return lastModifiedRecord?.ModifiedDate;
        }

        /// <summary>
        /// Creates a PhonePerson if it doesn't exist, or updates if it does.
        /// </summary>
        private async Task AddOrUpdatePhonePerson(
            string? userIam,
            DateTime updateTimestamp,
            string personIam,
            string phone,
            CancellationToken ct = default
        )
        {
            var phonePerson = await _context.PhonePerson.FindAsync(new object?[] { personIam.Trim() }, ct);
            if (phonePerson == null)
            {
                phonePerson = new PhonePerson
                {
                    PersonIam = personIam.Trim(),
                    Phone = phone.Trim(),
                    ModifiedDate = updateTimestamp,
                    ModifiedBy = userIam,
                };
                await _context.PhonePerson.AddAsync(phonePerson, ct);
            }
            else
            {
                phonePerson.Phone = phone.Trim();
                phonePerson.ModifiedDate = updateTimestamp;
                phonePerson.ModifiedBy = userIam;
            }
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Creates or updates both a dean/director and an admin staff
        /// member based on the data in request.
        /// </summary>
        private async Task AddOrUpdatePhonePeople(
            string? userIam,
            DateTime updateTimestamp,
            SVMUnitDataRequest request,
            CancellationToken ct = default
        )
        {
            if (!string.IsNullOrWhiteSpace(request.DeanIam))
            {
                await AddOrUpdatePhonePerson(
                    userIam,
                    updateTimestamp,
                    request.DeanIam,
                    request.DeanPhone,
                    ct
                );
            }
            if (!string.IsNullOrWhiteSpace(request.StaffIam))
            {
                await AddOrUpdatePhonePerson(
                    userIam,
                    updateTimestamp,
                    request.StaffIam,
                    request.StaffPhone,
                    ct
                );
            }
        }

        /// <summary>
        /// Updates UnitPerson records for both a dean/director
        /// and admin staff.
        /// </summary>
        private async Task UpdateUnitPeople(
            string? userIam,
            DateTime updateTimestamp,
            int unitId,
            SVMUnitDataRequest request,
            CancellationToken ct = default
        )
        {
            // If the new employees are already in the unit,
            // replace their records with the new ones.
            // Additionally, disable the records that were edited
            // on the front end. For a row add, the UnitPerson value will be -1
            // and not match any records. For edits, UnitPerson values
            // represent the rows being replaced.
            var oldUnitPeople = await _context.SVMUnitPerson.Where(
                p => p.UnitId == unitId &&
                (
                    p.PersonIam == request.DeanIam.Trim() ||
                    p.PersonIam == request.StaffIam.Trim() ||
                    p.UnitPersonId == request.DeanUnitPerson ||
                    p.UnitPersonId == request.StaffUnitPerson
                ) &&
                p.IsActive
            )
            .ToListAsync(ct);
            foreach (SVMUnitPerson unitPerson in oldUnitPeople)
            {
                unitPerson.ModifiedBy = userIam;
                unitPerson.ModifiedDate = updateTimestamp;
                unitPerson.IsActive = false;
            }
            await _context.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(request.DeanIam))
            {
                var newDeanUnitPerson = new SVMUnitPerson
                {
                    UnitId = unitId,
                    PersonIam = request.DeanIam.Trim(),
                    Office = request.Location.Trim(),
                    PosType = "Dean",
                    Interim = request.DeanInterim.Trim(),
                    ModifiedDate = updateTimestamp,
                    ModifiedBy = userIam,
                    IsActive = true
                };
                await _context.SVMUnitPerson.AddAsync(newDeanUnitPerson, ct);
                await _context.SaveChangesAsync(ct);
            }

            if (!string.IsNullOrWhiteSpace(request.StaffIam))
            {
                var newStaffUnitPerson = new SVMUnitPerson
                {
                    UnitId = unitId,
                    PersonIam = request.StaffIam.Trim(),
                    Office = request.Location.Trim(),
                    PosType = "Staff",
                    Interim = request.StaffInterim.Trim(),
                    ModifiedDate = updateTimestamp,
                    ModifiedBy = userIam,
                    IsActive = true
                };
                await _context.SVMUnitPerson.AddAsync(newStaffUnitPerson, ct);
                await _context.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// Adds or updates a row in the SVM list.
        /// Impacts Unit, UnitPerson, and PhonePerson.
        /// The differences in data in request distinguishes the behavior
        /// between add and update.
        /// </summary>
        public async Task AddOrUpdateUnitData(int unitId, SVMUnitDataRequest request, CancellationToken ct = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var userIam = _userHelper.GetCurrentUser()?.IamId;
            var updateTimestamp = DateTime.Now;
            var unit = await _context.SVMUnit.FindAsync(new object?[] { unitId }, ct);
            if (unit != null)
            {
                unit.Fax = request.Fax.Trim();
                unit.ModifiedBy = userIam;
                unit.ModifiedDate = updateTimestamp;
                await _context.SaveChangesAsync(ct);
                await AddOrUpdatePhonePeople(userIam, updateTimestamp, request, ct);
                await UpdateUnitPeople(userIam, updateTimestamp, unitId, request, ct);
            }
            else
            {
                throw new InvalidOperationException("Unit not found");
            }
            await transaction.CommitAsync(ct);
        }

        /// <summary>
        /// Removes one row of the SVM list. A row is a dean/director plus the admin staff shared
        /// by every row for that unit, so removing it deletes the leader and then the staff only
        /// once no other row still lists them. Both happen in one transaction: as two separate
        /// requests they could half-apply, leaving the caller unable to tell which part landed.
        ///
        /// entryId is the row key the list renders: the leader UnitPerson, or the admin staff
        /// for a unit that has staff but no active leader. Both cases reduce to the same rule,
        /// so there is no branch on which kind of row was named.
        ///
        /// Leaves SVMUnit and PhonePerson unchanged.
        /// </summary>
        public async Task DeleteUnitRow(int entryId, CancellationToken ct = default)
        {
            // Only an active row can be deleted. The message reaches the user as an error banner,
            // and through the UI the only way to miss is to act on a row another maintainer just
            // deleted, so it is worded for that reader.
            var rowEntry = await _context.SVMUnitPerson
                .FirstOrDefaultAsync(p => p.UnitPersonId == entryId && p.IsActive, ct);
            if (rowEntry == null)
            {
                throw new InvalidOperationException("That record has already been removed.");
            }

            var userIam = _userHelper.GetCurrentUser()?.IamId;
            var updateTimestamp = DateTime.Now;
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            void SoftDelete(SVMUnitPerson unitPerson)
            {
                unitPerson.ModifiedBy = userIam;
                unitPerson.ModifiedDate = updateTimestamp;
                unitPerson.IsActive = false;
            }

            if (rowEntry.PosType != "Staff")
            {
                SoftDelete(rowEntry);
            }
            await _context.SaveChangesAsync(ct);

            // The admin staff entry belongs to the unit, not to this row, so it survives as long
            // as any leader row still lists it.
            var leadersRemain = await _context.SVMUnitPerson
                .AsNoTracking()
                .AnyAsync(
                    p => p.UnitId == rowEntry.UnitId &&
                    p.PosType != null &&
                    p.PosType != "" &&
                    p.PosType != "Staff" &&
                    p.IsActive,
                    ct
                );
            if (!leadersRemain)
            {
                var staffEntries = await _context.SVMUnitPerson
                    .Where(p => p.UnitId == rowEntry.UnitId && p.PosType == "Staff" && p.IsActive)
                    .ToListAsync(ct);
                foreach (var staffEntry in staffEntries)
                {
                    SoftDelete(staffEntry);
                }
                await _context.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
        }
    }
}
