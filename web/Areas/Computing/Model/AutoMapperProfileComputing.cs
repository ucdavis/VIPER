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

            CreateMap<FakeUser, FakePerson>()
                .ForMember(dest => dest.PersonPKey, opt => opt.MapFrom(src => src.PKey))
                .ForMember(dest => dest.PersonDisplayFirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.PersonFirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.PersonDisplayLastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.PersonLastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.PersonClientid, opt => opt.MapFrom(src => src.ClientId))
                .ForMember(dest => dest.PersonTermCode, opt => opt.MapFrom(src => src.PKey.Substring(0, 6)));

            CreateMap<FakeUser, FakeId>()
                .ForMember(dest => dest.IdsPKey, opt => opt.MapFrom(src => src.PKey))
                .ForMember(dest => dest.IdsTermCode, opt => opt.MapFrom(src => src.PKey.Substring(0, 6)))
                .ForMember(dest => dest.IdsClientid, opt => opt.MapFrom(src => src.ClientId))
                .ForMember(dest => dest.IdsIamId, opt => opt.MapFrom(src => src.IamId))
                .ForMember(dest => dest.IdsMothraid, opt => opt.MapFrom(src => src.MothraId))
                .ForMember(dest => dest.IdsLoginid, opt => opt.MapFrom(src => src.LoginId))
                .ForMember(dest => dest.IdsMailid, opt => opt.MapFrom(src => src.MailId))
                .ForMember(dest => dest.IdsPidm, opt => opt.MapFrom(src => src.Pidm));


        }
    }
}
