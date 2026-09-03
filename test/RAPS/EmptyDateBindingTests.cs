using System.Text.Json;
using Viper.Areas.RAPS.Models;

namespace Viper.test.RAPS
{
    // Controller tests build the model in C#, so only these catch a body shape that 400s
    // during model binding, before the action runs.
    public class EmptyDateBindingTests
    {
        // The edit dialogs seed a cleared date as "" (formatDateForDateInput returns "" for
        // null), so an untouched permission with no dates posts empty strings.
        [Fact]
        public void MemberPermissionBody_WithEmptyDates_BindsThemAsNull()
        {
            var model = DeserializeMemberPermission(
                @"{""memberId"":""12345678"",""permissionId"":5,""access"":1,""startDate"":"""",""endDate"":""""}");

            Assert.Null(model.StartDate);
            Assert.Null(model.EndDate);
        }

        [Fact]
        public void MemberPermissionBody_WithRealDates_StillBinds()
        {
            var model = DeserializeMemberPermission(
                @"{""memberId"":""12345678"",""permissionId"":5,""access"":1,""startDate"":""2026-01-15T00:00:00"",""endDate"":""2026-06-30T00:00:00""}");

            Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Local), model.StartDate);
            Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), model.EndDate);
        }

        [Fact]
        public void MemberPermissionBody_WithNullOrOmittedDates_BindsAsNull()
        {
            var withNulls = DeserializeMemberPermission(
                @"{""memberId"":""12345678"",""permissionId"":5,""access"":1,""startDate"":null,""endDate"":null}");
            var omitted = DeserializeMemberPermission(
                @"{""memberId"":""12345678"",""permissionId"":5,""access"":1}");

            Assert.Null(withNulls.StartDate);
            Assert.Null(omitted.EndDate);
        }

        // MemberPermissionCreateUpdate goes back out on responses via PermissionClone, so the
        // converter has to write as well as read.
        [Fact]
        public void MemberPermission_RoundTripsThroughSerialization()
        {
            var model = new MemberPermissionCreateUpdate
            {
                MemberId = "12345678",
                PermissionId = 5,
                Access = 1,
                StartDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Local),
                EndDate = null
            };

            string json = JsonSerializer.Serialize(model, JsonSerializerOptions.Web);
            MemberPermissionCreateUpdate? result =
                JsonSerializer.Deserialize<MemberPermissionCreateUpdate>(json, JsonSerializerOptions.Web);

            Assert.NotNull(result);
            Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Local), result.StartDate);
            Assert.Null(result.EndDate);
        }

        // RoleMemberCreateUpdate uses DateOnly?, which rejects "" the same way.
        [Fact]
        public void RoleMemberBody_WithEmptyDates_BindsThemAsNull()
        {
            var model = DeserializeRoleMember(
                @"{""roleId"":3,""memberId"":""12345678"",""startDate"":"""",""endDate"":""""}");

            Assert.Null(model.StartDate);
            Assert.Null(model.EndDate);
        }

        [Fact]
        public void RoleMemberBody_WithRealDates_StillBinds()
        {
            var model = DeserializeRoleMember(
                @"{""roleId"":3,""memberId"":""12345678"",""startDate"":""2026-01-15"",""endDate"":""2026-06-30""}");

            Assert.Equal(new DateOnly(2026, 1, 15), model.StartDate);
            Assert.Equal(new DateOnly(2026, 6, 30), model.EndDate);
        }

        // JsonSerializerOptions.Web matches how MVC binds JSON bodies. The converters are
        // applied per property, so no extra options wiring is needed here.
        private static MemberPermissionCreateUpdate DeserializeMemberPermission(string body)
        {
            MemberPermissionCreateUpdate? model =
                JsonSerializer.Deserialize<MemberPermissionCreateUpdate>(body, JsonSerializerOptions.Web);

            Assert.NotNull(model);
            return model;
        }

        private static RoleMemberCreateUpdate DeserializeRoleMember(string body)
        {
            RoleMemberCreateUpdate? model =
                JsonSerializer.Deserialize<RoleMemberCreateUpdate>(body, JsonSerializerOptions.Web);

            Assert.NotNull(model);
            return model;
        }
    }
}
