using AutoMapper;
using Viper.Models.AAUD;

namespace Viper.Areas.Computing.Model
{
    public class AutoMapperProfileComputing : Profile
    {
        public AutoMapperProfileComputing() {
            CreateMap<FakePerson, FakeUser>()
                .ForMember(dest => dest.PKey, opt => opt.MapFrom(src => src.PersonPKey))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.PersonDisplayFirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.PersonDisplayLastName))
                .ForMember(dest => dest.LoginId, opt => opt.MapFrom(src => src.FakeId == null ? null : src.FakeId.IdsLoginid))
                .ForMember(dest => dest.MothraId, opt => opt.MapFrom(src => src.FakeId == null ? null : src.FakeId.IdsMothraid))
                .ForMember(dest => dest.MailId, opt => opt.MapFrom(src => src.FakeId == null ? null : src.FakeId.IdsMailid))
                .ForMember(dest => dest.IamId, opt => opt.MapFrom(src => src.FakeId == null ? null : src.FakeId.IdsIamId))
                .ForMember(dest => dest.ClientId, opt => opt.MapFrom(src => src.FakeId == null ? null : src.FakeId.IdsClientid))
                .ForMember(dest => dest.Pidm, opt => opt.MapFrom(src => src.FakeId == null ? null : src.FakeId.IdsPidm));
        }
    }
}
