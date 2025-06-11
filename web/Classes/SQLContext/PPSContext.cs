using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.PPS;

namespace Viper.Classes.SQLContext;

public partial class PPSContext : DbContext
{
    public PPSContext(DbContextOptions<PPSContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AbsenceCalendarDV> AbsenceCalendarDVs { get; set; }

    public virtual DbSet<AbsenceEventDV> AbsenceEventDVs { get; set; }

    public virtual DbSet<AbsenceEventFV> AbsenceEventFVs { get; set; }

    public virtual DbSet<AbsenceResultDV> AbsenceResultDVs { get; set; }

    public virtual DbSet<AbsenceResultFV> AbsenceResultFVs { get; set; }

    public virtual DbSet<AccountCodeDV> AccountCodeDVs { get; set; }

    public virtual DbSet<AuditOverride> AuditOverrides { get; set; }

    public virtual DbSet<CompensationDRateV> CompensationDRateVs { get; set; }

    public virtual DbSet<CompensationDV> CompensationDVs { get; set; }

    public virtual DbSet<CompensationFV> CompensationFVs { get; set; }

    public virtual DbSet<Ctlcad> Ctlcads { get; set; }

    public virtual DbSet<Ctltci> Ctltcis { get; set; }

    public virtual DbSet<Ctvhme> Ctvhmes { get; set; }

    public virtual DbSet<DepartmentBudgetEarnFV> DepartmentBudgetEarnFVs { get; set; }

    public virtual DbSet<DepartmentDV> DepartmentDVs { get; set; }

    public virtual DbSet<DiversEthnicityDV> DiversEthnicityDVs { get; set; }

    public virtual DbSet<Dvtloa> Dvtloas { get; set; }

    public virtual DbSet<EarnCodeDV> EarnCodeDVs { get; set; }

    public virtual DbSet<Edbeffrpt> Edbeffrpts { get; set; }

    public virtual DbSet<EdbeffrptLog> EdbeffrptLogs { get; set; }

    public virtual DbSet<EdbperV> EdbperVs { get; set; }

    public virtual DbSet<EdbperVc> EdbperVcs { get; set; }

    public virtual DbSet<EmployeeDV> EmployeeDVs { get; set; }

    public virtual DbSet<EmployeeHistory> EmployeeHistories { get; set; }

    public virtual DbSet<EthnicityGender> EthnicityGenders { get; set; }

    public virtual DbSet<EthnicityGender20210201> EthnicityGender20210201s { get; set; }

    public virtual DbSet<Export> Exports { get; set; }

    public virtual DbSet<FurloughTarget> FurloughTargets { get; set; }

    public virtual DbSet<GoAnywhereLog> GoAnywhereLogs { get; set; }

    public virtual DbSet<HistServiceCredit> HistServiceCredits { get; set; }

    public virtual DbSet<HistappdisV> HistappdisVs { get; set; }

    public virtual DbSet<HistloaV> HistloaVs { get; set; }

    public virtual DbSet<JobActionDV> JobActionDVs { get; set; }

    public virtual DbSet<JobCodeDV> JobCodeDVs { get; set; }

    public virtual DbSet<JobDV> JobDVs { get; set; }

    public virtual DbSet<JobFV> JobFVs { get; set; }

    public virtual DbSet<JobHistory> JobHistories { get; set; }

    public virtual DbSet<JobHistoryDetail> JobHistoryDetails { get; set; }

    public virtual DbSet<JobOverride> JobOverrides { get; set; }

    public virtual DbSet<JobStatusDV> JobStatusDVs { get; set; }

    public virtual DbSet<JpmFV> JpmFVs { get; set; }

    public virtual DbSet<JpmJpItemDV> JpmJpItemDVs { get; set; }

    public virtual DbSet<OdsEmployeeFlagsDV> OdsEmployeeFlagsDVs { get; set; }

    public virtual DbSet<OrganizationDV> OrganizationDVs { get; set; }

    public virtual DbSet<PayGroupDV> PayGroupDVs { get; set; }

    public virtual DbSet<PayItemNameDV> PayItemNameDVs { get; set; }

    public virtual DbSet<PositionDV> PositionDVs { get; set; }

    public virtual DbSet<PrimaryJobsDV> PrimaryJobsDVs { get; set; }

    public virtual DbSet<Prj2EmpidToPpsid> Prj2EmpidToPpsids { get; set; }

    public virtual DbSet<PsAcctCdTblV> PsAcctCdTblVs { get; set; }

    public virtual DbSet<PsActionTblV> PsActionTblVs { get; set; }

    public virtual DbSet<PsActnReasonTblV> PsActnReasonTblVs { get; set; }

    public virtual DbSet<PsAddlPayDataV> PsAddlPayDataVs { get; set; }

    public virtual DbSet<PsAddressTypTblV> PsAddressTypTblVs { get; set; }

    public virtual DbSet<PsAddressesV> PsAddressesVs { get; set; }

    public virtual DbSet<PsCitizenStsTblV> PsCitizenStsTblVs { get; set; }

    public virtual DbSet<PsCitizenshipV> PsCitizenshipVs { get; set; }

    public virtual DbSet<PsCompRatecdTblV> PsCompRatecdTblVs { get; set; }

    public virtual DbSet<PsCompensationV> PsCompensationVs { get; set; }

    public virtual DbSet<PsCountryTblV> PsCountryTblVs { get; set; }

    public virtual DbSet<PsDeptBudgetDtV> PsDeptBudgetDtVs { get; set; }

    public virtual DbSet<PsDeptBudgetErnV> PsDeptBudgetErnVs { get; set; }

    public virtual DbSet<PsDeptTblV> PsDeptTblVs { get; set; }

    public virtual DbSet<PsDiversEthnicV> PsDiversEthnicVs { get; set; }

    public virtual DbSet<PsEarningsBalV> PsEarningsBalVs { get; set; }

    public virtual DbSet<PsEarningsTblV> PsEarningsTblVs { get; set; }

    public virtual DbSet<PsEmailAddressesV> PsEmailAddressesVs { get; set; }

    public virtual DbSet<PsEmplClassTblV> PsEmplClassTblVs { get; set; }

    public virtual DbSet<PsErnProgramTblV> PsErnProgramTblVs { get; set; }

    public virtual DbSet<PsEthnicGrpTblV> PsEthnicGrpTblVs { get; set; }

    public virtual DbSet<PsGpAbsEaStaV> PsGpAbsEaStaVs { get; set; }

    public virtual DbSet<PsGpAbsEaV> PsGpAbsEaVs { get; set; }

    public virtual DbSet<PsGpAbsEventV> PsGpAbsEventVs { get; set; }

    public virtual DbSet<PsGpAbsReasonV> PsGpAbsReasonVs { get; set; }

    public virtual DbSet<PsGpPinV> PsGpPinVs { get; set; }

    public virtual DbSet<PsGpRsltAcumV> PsGpRsltAcumVs { get; set; }

    public virtual DbSet<PsJobV> PsJobVs { get; set; }

    public virtual DbSet<PsJobcodeTblV> PsJobcodeTblVs { get; set; }

    public virtual DbSet<PsJpmCatItemsV> PsJpmCatItemsVs { get; set; }

    public virtual DbSet<PsJpmCatTypesV> PsJpmCatTypesVs { get; set; }

    public virtual DbSet<PsJpmJpItemsV> PsJpmJpItemsVs { get; set; }

    public virtual DbSet<PsJpmProfileV> PsJpmProfileVs { get; set; }

    public virtual DbSet<PsPerOrgInstV> PsPerOrgInstVs { get; set; }

    public virtual DbSet<PsPersDataEffdtV> PsPersDataEffdtVs { get; set; }

    public virtual DbSet<PsPersonV> PsPersonVs { get; set; }

    public virtual DbSet<PsPersonalPhoneV> PsPersonalPhoneVs { get; set; }

    public virtual DbSet<PsPositionDataV> PsPositionDataVs { get; set; }

    public virtual DbSet<PsPrimaryJobsV> PsPrimaryJobsVs { get; set; }

    public virtual DbSet<PsUcAmSsRcdV> PsUcAmSsRcdVs { get; set; }

    public virtual DbSet<PsUcAmSsTblV> PsUcAmSsTblVs { get; set; }

    public virtual DbSet<PsUcCtoOscV> PsUcCtoOscVs { get; set; }

    public virtual DbSet<PsUcExtSystemV> PsUcExtSystemVs { get; set; }

    public virtual DbSet<PsUcFundAttribV> PsUcFundAttribVs { get; set; }

    public virtual DbSet<PsUcGpAbsEaV> PsUcGpAbsEaVs { get; set; }

    public virtual DbSet<PsUcJobCodeTblV> PsUcJobCodeTblVs { get; set; }

    public virtual DbSet<PsUcJobCodesV> PsUcJobCodesVs { get; set; }

    public virtual DbSet<PsUcJobGrpDescV> PsUcJobGrpDescVs { get; set; }

    public virtual DbSet<PsUcSsDisclosurV> PsUcSsDisclosurVs { get; set; }

    public virtual DbSet<PsUcdDmPsNamesPrefVnamesV> PsUcdDmPsNamesPrefVnamesVs { get; set; }

    public virtual DbSet<PsUnionTblV> PsUnionTblVs { get; set; }

    public virtual DbSet<PsVisaPmtDataV> PsVisaPmtDataVs { get; set; }

    public virtual DbSet<PsxlatitemV> PsxlatitemVs { get; set; }

    public virtual DbSet<ServiceCredit> ServiceCredits { get; set; }

    public virtual DbSet<ServiceCreditUnit> ServiceCreditUnits { get; set; }

    public virtual DbSet<TimeDailyDV> TimeDailyDVs { get; set; }

    public virtual DbSet<TitlecodeGroup> TitlecodeGroups { get; set; }

    public virtual DbSet<UcdEmployeeFlagsDOverride> UcdEmployeeFlagsDOverrides { get; set; }

    public virtual DbSet<UcdEmployeeFlagsDV> UcdEmployeeFlagsDVs { get; set; }

    public virtual DbSet<UcdFauDV> UcdFauDVs { get; set; }

    public virtual DbSet<UcdOrganizationDV> UcdOrganizationDVs { get; set; }

    public virtual DbSet<UcdaptdisV> UcdaptdisVs { get; set; }

    public virtual DbSet<UcpathItemNote> UcpathItemNotes { get; set; }

    public virtual DbSet<UcpathMissingPerson> UcpathMissingPeople { get; set; }

    public virtual DbSet<UcpathMissingPerson20190821> UcpathMissingPerson20190821s { get; set; }

    public virtual DbSet<UcpathOverride> UcpathOverrides { get; set; }

    public virtual DbSet<UcpathVerificationItem> UcpathVerificationItems { get; set; }

    public virtual DbSet<UcpathmissingpersonBk> UcpathmissingpersonBks { get; set; }

    public virtual DbSet<UnionDV> UnionDVs { get; set; }

    public virtual DbSet<UpdateLog> UpdateLogs { get; set; }

    public virtual DbSet<VisaPermitDataDV> VisaPermitDataDVs { get; set; }

    public virtual DbSet<VwAccrual> VwAccruals { get; set; }

    public virtual DbSet<VwAccrualsCdm> VwAccrualsCdms { get; set; }

    public virtual DbSet<VwAllJobPosOrg> VwAllJobPosOrgs { get; set; }

    public virtual DbSet<VwEmpJobPosOrg> VwEmpJobPosOrgs { get; set; }

    public virtual DbSet<VwEmployee> VwEmployees { get; set; }

    public virtual DbSet<VwJobCodeAndGroup> VwJobCodeAndGroups { get; set; }

    public virtual DbSet<VwLoa> VwLoas { get; set; }

    public virtual DbSet<VwPerson> VwPeople { get; set; }

    public virtual DbSet<VwPersonAccrual> VwPersonAccruals { get; set; }

    public virtual DbSet<VwPersonJobPosition> VwPersonJobPositions { get; set; }

    public virtual DbSet<VwPersonJobPositionAll> VwPersonJobPositionAlls { get; set; }

    public virtual DbSet<VwRankStepEthnicityPayRate> VwRankStepEthnicityPayRates { get; set; }

    public virtual DbSet<VwStipend> VwStipends { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AbsenceCalendarDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ABSENCE_CALENDAR_D_V");

            entity.Property(e => e.AbsCalCalctnThrghDt).HasColumnName("ABS_CAL_CALCTN_THRGH_DT");
            entity.Property(e => e.AbsCalDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_CAL_D_KEY");
            entity.Property(e => e.AbsCalId)
                .HasMaxLength(18)
                .HasColumnName("ABS_CAL_ID");
            entity.Property(e => e.AbsCalPaygrpCd)
                .HasMaxLength(10)
                .HasColumnName("ABS_CAL_PAYGRP_CD");
            entity.Property(e => e.AbsCalPerBegDt).HasColumnName("ABS_CAL_PER_BEG_DT");
            entity.Property(e => e.AbsCalPerDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_CAL_PER_DESC");
            entity.Property(e => e.AbsCalPerEndDt).HasColumnName("ABS_CAL_PER_END_DT");
            entity.Property(e => e.AbsCalPerFreqId)
                .HasMaxLength(5)
                .HasColumnName("ABS_CAL_PER_FREQ_ID");
            entity.Property(e => e.AbsCalPerId)
                .HasMaxLength(10)
                .HasColumnName("ABS_CAL_PER_ID");
            entity.Property(e => e.AbsCalPerShortDesc)
                .HasMaxLength(10)
                .HasColumnName("ABS_CAL_PER_SHORT_DESC");
            entity.Property(e => e.AbsCalPyeSelctOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_CAL_PYE_SELCT_OPTN_DESC");
            entity.Property(e => e.AbsCalPyeSelctOptnFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_CAL_PYE_SELCT_OPTN_FLG");
            entity.Property(e => e.AbsCalPyeSelctPlFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_CAL_PYE_SELCT_PL_FLG");
            entity.Property(e => e.AbsCalPyeSelctRtoFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_CAL_PYE_SELCT_RTO_FLG");
            entity.Property(e => e.AbsCalPymtDt).HasColumnName("ABS_CAL_PYMT_DT");
            entity.Property(e => e.AbsCalRunType)
                .HasMaxLength(10)
                .HasColumnName("ABS_CAL_RUN_TYPE");
            entity.Property(e => e.AbsCalSelctCritOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_CAL_SELCT_CRIT_OPTN_DESC");
            entity.Property(e => e.AbsCalSelctCritOptnFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_CAL_SELCT_CRIT_OPTN_FLG");
            entity.Property(e => e.AbsCalSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("ABS_CAL__SVM_IS_MOSTRECENT");
            entity.Property(e => e.AbsCalSvmSeqMrf).HasColumnName("ABS_CAL__SVM_SEQ_MRF");
            entity.Property(e => e.AbsCalSvmSeqNum).HasColumnName("ABS_CAL__SVM_SEQ_NUM");
            entity.Property(e => e.AbsCalTlPerId)
                .HasMaxLength(10)
                .HasColumnName("ABS_CAL_TL_PER_ID");
            entity.Property(e => e.AbsCalTrgtCalId)
                .HasMaxLength(18)
                .HasColumnName("ABS_CAL_TRGT_CAL_ID");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<AbsenceEventDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ABSENCE_EVENT_D_V");

            entity.Property(e => e.AbsEvntActnPrcsDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_EVNT_ACTN_PRCS_DESC");
            entity.Property(e => e.AbsEvntActnPrcsInd)
                .HasMaxLength(1)
                .HasColumnName("ABS_EVNT_ACTN_PRCS_IND");
            entity.Property(e => e.AbsEvntCnfgrtn1Cd)
                .HasMaxLength(10)
                .HasColumnName("ABS_EVNT_CNFGRTN1_CD");
            entity.Property(e => e.AbsEvntCnfgrtn2Cd)
                .HasMaxLength(10)
                .HasColumnName("ABS_EVNT_CNFGRTN2_CD");
            entity.Property(e => e.AbsEvntCnfgrtn3Cd)
                .HasMaxLength(10)
                .HasColumnName("ABS_EVNT_CNFGRTN3_CD");
            entity.Property(e => e.AbsEvntCnfgrtn4Cd)
                .HasMaxLength(10)
                .HasColumnName("ABS_EVNT_CNFGRTN4_CD");
            entity.Property(e => e.AbsEvntDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_EVNT_D_KEY");
            entity.Property(e => e.AbsEvntEntrySrcCd)
                .HasMaxLength(1)
                .HasColumnName("ABS_EVNT_ENTRY_SRC_CD");
            entity.Property(e => e.AbsEvntEntrySrcDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_EVNT_ENTRY_SRC_DESC");
            entity.Property(e => e.AbsEvntMgrAprvFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_EVNT_MGR_APRV_FLG");
            entity.Property(e => e.AbsEvntRsnCd)
                .HasMaxLength(3)
                .HasColumnName("ABS_EVNT_RSN_CD");
            entity.Property(e => e.AbsEvntVoidedFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_EVNT_VOIDED_FLG");
            entity.Property(e => e.AbsEvntWrkflwStatCd)
                .HasMaxLength(1)
                .HasColumnName("ABS_EVNT_WRKFLW_STAT_CD");
            entity.Property(e => e.AbsEvntWrkflwStatDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_EVNT_WRKFLW_STAT_DESC");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<AbsenceEventFV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ABSENCE_EVENT_F_V");

            entity.Property(e => e.AbsEvntDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_EVNT_D_KEY");
            entity.Property(e => e.AbsEvntFCalRunId)
                .HasMaxLength(18)
                .HasColumnName("ABS_EVNT_F_CAL_RUN_ID");
            entity.Property(e => e.AbsEvntFCnfgrtn1Num)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_EVNT_F_CNFGRTN1_NUM");
            entity.Property(e => e.AbsEvntFCnfgrtn2Num)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_EVNT_F_CNFGRTN2_NUM");
            entity.Property(e => e.AbsEvntFCnfgrtn3Num)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_EVNT_F_CNFGRTN3_NUM");
            entity.Property(e => e.AbsEvntFCnfgrtn4Num)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_EVNT_F_CNFGRTN4_NUM");
            entity.Property(e => e.AbsEvntFCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_EVNT_F_CNT");
            entity.Property(e => e.AbsEvntFDurtnAmt)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("ABS_EVNT_F_DURTN_AMT");
            entity.Property(e => e.AbsEvntFDurtnDys)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("ABS_EVNT_F_DURTN_DYS");
            entity.Property(e => e.AbsEvntFDurtnHrs)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("ABS_EVNT_F_DURTN_HRS");
            entity.Property(e => e.AbsEvntFEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_EVNT_F_EMP_REC_NUM");
            entity.Property(e => e.AbsEvntFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_EVNT_F_KEY");
            entity.Property(e => e.AbsEvntFOvrdAdjstmtAmt)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("ABS_EVNT_F_OVRD_ADJSTMT_AMT");
            entity.Property(e => e.AbsEvntFOvrdEntlmntAmt)
                .HasColumnType("numeric(10, 6)")
                .HasColumnName("ABS_EVNT_F_OVRD_ENTLMNT_AMT");
            entity.Property(e => e.AbsEvntFPayeeRunNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_EVNT_F_PAYEE_RUN_NUM");
            entity.Property(e => e.AbsFSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("ABS_F__SVM_IS_MOSTRECENT");
            entity.Property(e => e.AbsFSvmSeqMrf).HasColumnName("ABS_F__SVM_SEQ_MRF");
            entity.Property(e => e.AbsFSvmSeqNum).HasColumnName("ABS_F__SVM_SEQ_NUM");
            entity.Property(e => e.BegDt).HasColumnName("BEG_DT");
            entity.Property(e => e.BegDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BEG_DT_KEY");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EndDt).HasColumnName("END_DT");
            entity.Property(e => e.EndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("END_DT_KEY");
            entity.Property(e => e.EventCnfgrtn1DtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EVENT_CNFGRTN1_DT_KEY");
            entity.Property(e => e.EventCnfgrtn2DtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EVENT_CNFGRTN2_DT_KEY");
            entity.Property(e => e.EventCnfgrtn3DtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EVENT_CNFGRTN3_DT_KEY");
            entity.Property(e => e.EventCnfgrtn4DtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EVENT_CNFGRTN4_DT_KEY");
            entity.Property(e => e.FirstPrcsDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FIRST_PRCS_DT_KEY");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_KEY");
            entity.Property(e => e.LastUpdtDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("LAST_UPDT_DT_KEY");
            entity.Property(e => e.OrigBegDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORIG_BEG_DT_KEY");
            entity.Property(e => e.PinDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_D_KEY");
            entity.Property(e => e.PrcsDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRCS_DT_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<AbsenceResultDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ABSENCE_RESULT_D_V");

            entity.Property(e => e.AbsRsltAcumPerOptnCd)
                .HasMaxLength(1)
                .HasColumnName("ABS_RSLT_ACUM_PER_OPTN_CD");
            entity.Property(e => e.AbsRsltAcumPerOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_RSLT_ACUM_PER_OPTN_DESC");
            entity.Property(e => e.AbsRsltAcumTypeCd)
                .HasMaxLength(1)
                .HasColumnName("ABS_RSLT_ACUM_TYPE_CD");
            entity.Property(e => e.AbsRsltAcumTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_RSLT_ACUM_TYPE_DESC");
            entity.Property(e => e.AbsRsltCalledInSgmntFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_RSLT_CALLED_IN_SGMNT_FLG");
            entity.Property(e => e.AbsRsltCntryCd)
                .HasMaxLength(3)
                .HasColumnName("ABS_RSLT_CNTRY_CD");
            entity.Property(e => e.AbsRsltCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("ABS_RSLT_CNTRY_DESC");
            entity.Property(e => e.AbsRsltCrctvRetroMthdFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_RSLT_CRCTV_RETRO_MTHD_FLG");
            entity.Property(e => e.AbsRsltDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_D_KEY");
            entity.Property(e => e.AbsRsltSubUserAcumltr1Nm)
                .HasMaxLength(25)
                .HasColumnName("ABS_RSLT_SUB_USER_ACUMLTR1_NM");
            entity.Property(e => e.AbsRsltSubUserAcumltr2Nm)
                .HasMaxLength(25)
                .HasColumnName("ABS_RSLT_SUB_USER_ACUMLTR2_NM");
            entity.Property(e => e.AbsRsltSubUserAcumltr3Nm)
                .HasMaxLength(25)
                .HasColumnName("ABS_RSLT_SUB_USER_ACUMLTR3_NM");
            entity.Property(e => e.AbsRsltSubUserAcumltr4Nm)
                .HasMaxLength(25)
                .HasColumnName("ABS_RSLT_SUB_USER_ACUMLTR4_NM");
            entity.Property(e => e.AbsRsltSubUserAcumltr5Nm)
                .HasMaxLength(25)
                .HasColumnName("ABS_RSLT_SUB_USER_ACUMLTR5_NM");
            entity.Property(e => e.AbsRsltSubUserAcumltr6Nm)
                .HasMaxLength(25)
                .HasColumnName("ABS_RSLT_SUB_USER_ACUMLTR6_NM");
            entity.Property(e => e.AbsRsltValdInSgmntFlg)
                .HasMaxLength(1)
                .HasColumnName("ABS_RSLT_VALD_IN_SGMNT_FLG");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<AbsenceResultFV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ABSENCE_RESULT_F_V");

            entity.Property(e => e.AbsCalDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_CAL_D_KEY");
            entity.Property(e => e.AbsRsltDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_D_KEY");
            entity.Property(e => e.AbsRsltFAcumEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_ACUM_EMP_REC_NUM");
            entity.Property(e => e.AbsRsltFCalRunId)
                .HasMaxLength(18)
                .HasColumnName("ABS_RSLT_F_CAL_RUN_ID");
            entity.Property(e => e.AbsRsltFCalcRsltValRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_RSLT_F_CALC_RSLT_VAL_RT");
            entity.Property(e => e.AbsRsltFCalcValRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_RSLT_F_CALC_VAL_RT");
            entity.Property(e => e.AbsRsltFCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_CNT");
            entity.Property(e => e.AbsRsltFEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_EMP_REC_NUM");
            entity.Property(e => e.AbsRsltFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_KEY");
            entity.Property(e => e.AbsRsltFOrigCalRunId)
                .HasMaxLength(18)
                .HasColumnName("ABS_RSLT_F_ORIG_CAL_RUN_ID");
            entity.Property(e => e.AbsRsltFRsltSgmntNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_RSLT_SGMNT_NUM");
            entity.Property(e => e.AbsRsltFSeq8Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_SEQ8_NUM");
            entity.Property(e => e.AbsRsltFSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("ABS_RSLT_F__SVM_IS_MOSTRECENT");
            entity.Property(e => e.AbsRsltFSvmSeqMrf).HasColumnName("ABS_RSLT_F__SVM_SEQ_MRF");
            entity.Property(e => e.AbsRsltFSvmSeqNum).HasColumnName("ABS_RSLT_F__SVM_SEQ_NUM");
            entity.Property(e => e.AbsRsltFUserAdjstValRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_RSLT_F_USER_ADJST_VAL_RT");
            entity.Property(e => e.AcumFromDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACUM_FROM_DT_KEY");
            entity.Property(e => e.AcumThrghDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACUM_THRGH_DT_KEY");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_KEY");
            entity.Property(e => e.ParentPinDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PARENT_PIN_D_KEY");
            entity.Property(e => e.PinDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_D_KEY");
            entity.Property(e => e.SliceBegDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SLICE_BEG_DT_KEY");
            entity.Property(e => e.SliceEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SLICE_END_DT_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<AccountCodeDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ACCOUNT_CODE_D_V");

            entity.Property(e => e.AcctCd)
                .HasMaxLength(25)
                .HasColumnName("ACCT_CD");
            entity.Property(e => e.AcctCdAcctId)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_ACCT_ID");
            entity.Property(e => e.AcctCdActvtyId)
                .HasMaxLength(15)
                .HasColumnName("ACCT_CD_ACTVTY_ID");
            entity.Property(e => e.AcctCdAffiliateId)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_AFFILIATE_ID");
            entity.Property(e => e.AcctCdAffiliateIntra1Id)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_AFFILIATE_INTRA1_ID");
            entity.Property(e => e.AcctCdAffiliateIntra2Id)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_AFFILIATE_INTRA2_ID");
            entity.Property(e => e.AcctCdAltAcctCd)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_ALT_ACCT_CD");
            entity.Property(e => e.AcctCdBdgtRefrncCd)
                .HasMaxLength(8)
                .HasColumnName("ACCT_CD_BDGT_REFRNC_CD");
            entity.Property(e => e.AcctCdBusUnitProjCostId)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_BUS_UNIT_PROJ_COST_ID");
            entity.Property(e => e.AcctCdChrtfld1Cd)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_CHRTFLD1_CD");
            entity.Property(e => e.AcctCdChrtfld2Cd)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_CHRTFLD2_CD");
            entity.Property(e => e.AcctCdChrtfld3Cd)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_CHRTFLD3_CD");
            entity.Property(e => e.AcctCdClassFieldCd)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_CLASS_FIELD_CD");
            entity.Property(e => e.AcctCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACCT_CD_D_KEY");
            entity.Property(e => e.AcctCdDeptChrtfldId)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_DEPT_CHRTFLD_ID");
            entity.Property(e => e.AcctCdDesc)
                .HasMaxLength(30)
                .HasColumnName("ACCT_CD_DESC");
            entity.Property(e => e.AcctCdDirectChrgFlg)
                .HasMaxLength(1)
                .HasColumnName("ACCT_CD_DIRECT_CHRG_FLG");
            entity.Property(e => e.AcctCdEncmbrncAcctId)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_ENCMBRNC_ACCT_ID");
            entity.Property(e => e.AcctCdFdmHash)
                .HasMaxLength(31)
                .HasColumnName("ACCT_CD_FDM_HASH");
            entity.Property(e => e.AcctCdFundCd)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_FUND_CD");
            entity.Property(e => e.AcctCdOpertgUnitCd)
                .HasMaxLength(8)
                .HasColumnName("ACCT_CD_OPERTG_UNIT_CD");
            entity.Property(e => e.AcctCdPreEncmbrncAcctId)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_PRE_ENCMBRNC_ACCT_ID");
            entity.Property(e => e.AcctCdProductId)
                .HasMaxLength(6)
                .HasColumnName("ACCT_CD_PRODUCT_ID");
            entity.Property(e => e.AcctCdProgCd)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_PROG_CD");
            entity.Property(e => e.AcctCdProjId)
                .HasMaxLength(15)
                .HasColumnName("ACCT_CD_PROJ_ID");
            entity.Property(e => e.AcctCdProrateLiabilityFlg)
                .HasMaxLength(1)
                .HasColumnName("ACCT_CD_PRORATE_LIABILITY_FLG");
            entity.Property(e => e.AcctCdResrcCtgyId)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_RESRC_CTGY_ID");
            entity.Property(e => e.AcctCdResrcSubCtgyId)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_RESRC_SUB_CTGY_ID");
            entity.Property(e => e.AcctCdResrcTypeCd)
                .HasMaxLength(5)
                .HasColumnName("ACCT_CD_RESRC_TYPE_CD");
            entity.Property(e => e.AcctCdShortDesc)
                .HasMaxLength(10)
                .HasColumnName("ACCT_CD_SHORT_DESC");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<AuditOverride>(entity =>
        {
            entity.ToTable("auditOverride");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.After).HasColumnName("after");
            entity.Property(e => e.Area)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("area");
            entity.Property(e => e.Before).HasColumnName("before");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("modifiedBy");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<CompensationDRateV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("COMPENSATION_D_RATE_V");

            entity.Property(e => e.CmpCalcByCd)
                .HasMaxLength(2)
                .HasColumnName("CMP_CALC_BY_CD");
            entity.Property(e => e.CmpCalcByDesc)
                .HasMaxLength(30)
                .HasColumnName("CMP_CALC_BY_DESC");
            entity.Property(e => e.CmpNonUpdFlg)
                .HasMaxLength(1)
                .HasColumnName("CMP_NON_UPD_FLG");
            entity.Property(e => e.CmpPaySwtchFlg)
                .HasMaxLength(1)
                .HasColumnName("CMP_PAY_SWTCH_FLG");
            entity.Property(e => e.CompBasePaySwtchCd)
                .HasMaxLength(1)
                .HasColumnName("COMP_BASE_PAY_SWTCH_CD");
            entity.Property(e => e.CompEarnCd)
                .HasMaxLength(3)
                .HasColumnName("COMP_EARN_CD");
            entity.Property(e => e.CompEarnDesc)
                .HasMaxLength(30)
                .HasColumnName("COMP_EARN_DESC");
            entity.Property(e => e.CompFreqCd)
                .HasMaxLength(5)
                .HasColumnName("COMP_FREQ_CD");
            entity.Property(e => e.CompFreqDesc)
                .HasMaxLength(30)
                .HasColumnName("COMP_FREQ_DESC");
            entity.Property(e => e.CompPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("COMP_PCT");
            entity.Property(e => e.CompRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("COMP_RT");
            entity.Property(e => e.CompRtCd)
                .HasMaxLength(6)
                .HasColumnName("COMP_RT_CD");
            entity.Property(e => e.CompRtDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_RT_D_KEY");
            entity.Property(e => e.CompRtDesc)
                .HasMaxLength(30)
                .HasColumnName("COMP_RT_DESC");
            entity.Property(e => e.CompRtEffDt).HasColumnName("COMP_RT_EFF_DT");
            entity.Property(e => e.CompRtEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("COMP_RT_EFF_STAT_CD");
            entity.Property(e => e.CompRtShortDesc)
                .HasMaxLength(10)
                .HasColumnName("COMP_RT_SHORT_DESC");
            entity.Property(e => e.CompRtTypeCd)
                .HasMaxLength(2)
                .HasColumnName("COMP_RT_TYPE_CD");
            entity.Property(e => e.CompRtTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("COMP_RT_TYPE_DESC");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.FteIndFlg)
                .HasMaxLength(1)
                .HasColumnName("FTE_IND_FLG");
            entity.Property(e => e.LookupId)
                .HasMaxLength(15)
                .HasColumnName("LOOKUP_ID");
            entity.Property(e => e.RtClassCd)
                .HasMaxLength(6)
                .HasColumnName("RT_CLASS_CD");
            entity.Property(e => e.SlryPkgWarnFlg)
                .HasMaxLength(1)
                .HasColumnName("SLRY_PKG_WARN_FLG");
            entity.Property(e => e.SnrtyCalcFlg)
                .HasMaxLength(1)
                .HasColumnName("SNRTY_CALC_FLG");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
            entity.Property(e => e.UseHighstRtSwtchCd)
                .HasMaxLength(1)
                .HasColumnName("USE_HIGHST_RT_SWTCH_CD");
        });

        modelBuilder.Entity<CompensationDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("COMPENSATION_D_V");

            entity.Property(e => e.CompDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_D_KEY");
            entity.Property(e => e.CompFreqCd)
                .HasMaxLength(5)
                .HasColumnName("COMP_FREQ_CD");
            entity.Property(e => e.CompFreqDesc)
                .HasMaxLength(30)
                .HasColumnName("COMP_FREQ_DESC");
            entity.Property(e => e.CompntSrcCd)
                .HasMaxLength(1)
                .HasColumnName("COMPNT_SRC_CD");
            entity.Property(e => e.CompntSrcDesc)
                .HasMaxLength(30)
                .HasColumnName("COMPNT_SRC_DESC");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.FteIndFlg)
                .HasMaxLength(1)
                .HasColumnName("FTE_IND_FLG");
            entity.Property(e => e.ManlSwtchFlg)
                .HasMaxLength(1)
                .HasColumnName("MANL_SWTCH_FLG");
            entity.Property(e => e.RtCdGrpCd)
                .HasMaxLength(6)
                .HasColumnName("RT_CD_GRP_CD");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<CompensationFV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("COMPENSATION_F_V");

            entity.Property(e => e.ChngAmt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CHNG_AMT");
            entity.Property(e => e.ChngPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("CHNG_PCT");
            entity.Property(e => e.ChngPts)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CHNG_PTS");
            entity.Property(e => e.CnvrtCompRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CNVRT_COMP_RT");
            entity.Property(e => e.CompDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_D_KEY");
            entity.Property(e => e.CompFCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_F_CNT");
            entity.Property(e => e.CompFEffSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_F_EFF_SEQ_NUM");
            entity.Property(e => e.CompFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_F_KEY");
            entity.Property(e => e.CompPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("COMP_PCT");
            entity.Property(e => e.CompRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("COMP_RT");
            entity.Property(e => e.CompRtDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_RT_D_KEY");
            entity.Property(e => e.CompRtPts)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_RT_PTS");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EffDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFF_DT_KEY");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<Ctlcad>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CTLCAD");

            entity.Property(e => e.CadB1PayDay)
                .HasMaxLength(1)
                .HasColumnName("CAD_B1_PAY_DAY");
            entity.Property(e => e.CadB1PayEnd)
                .HasMaxLength(1)
                .HasColumnName("CAD_B1_PAY_END");
            entity.Property(e => e.CadB2PayDay)
                .HasMaxLength(1)
                .HasColumnName("CAD_B2_PAY_DAY");
            entity.Property(e => e.CadB2PayEnd)
                .HasMaxLength(1)
                .HasColumnName("CAD_B2_PAY_END");
            entity.Property(e => e.CadDay)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("CAD_DAY");
            entity.Property(e => e.CadDayType)
                .HasMaxLength(1)
                .HasColumnName("CAD_DAY_TYPE");
            entity.Property(e => e.CadLastAction)
                .HasMaxLength(1)
                .HasColumnName("CAD_LAST_ACTION");
            entity.Property(e => e.CadLastActionDt).HasColumnName("CAD_LAST_ACTION_DT");
            entity.Property(e => e.CadMaPayDay)
                .HasMaxLength(1)
                .HasColumnName("CAD_MA_PAY_DAY");
            entity.Property(e => e.CadMaPayEnd)
                .HasMaxLength(1)
                .HasColumnName("CAD_MA_PAY_END");
            entity.Property(e => e.CadMoPayDay)
                .HasMaxLength(1)
                .HasColumnName("CAD_MO_PAY_DAY");
            entity.Property(e => e.CadMoPayEnd)
                .HasMaxLength(1)
                .HasColumnName("CAD_MO_PAY_END");
            entity.Property(e => e.CadMonth)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("CAD_MONTH");
            entity.Property(e => e.CadSmPayDay)
                .HasMaxLength(1)
                .HasColumnName("CAD_SM_PAY_DAY");
            entity.Property(e => e.CadSmPayEnd)
                .HasMaxLength(1)
                .HasColumnName("CAD_SM_PAY_END");
            entity.Property(e => e.CadYear)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("CAD_YEAR");
        });

        modelBuilder.Entity<Ctltci>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CTLTCI");

            entity.Property(e => e.TciAllFlag)
                .HasMaxLength(1)
                .HasColumnName("TCI_ALL_FLAG");
            entity.Property(e => e.TciByAgreemFlag)
                .HasMaxLength(1)
                .HasColumnName("TCI_BY_AGREEM_FLAG");
            entity.Property(e => e.TciCiEffDt).HasColumnName("TCI_CI_EFF_DT");
            entity.Property(e => e.TciCtoOsc)
                .HasMaxLength(3)
                .HasColumnName("TCI_CTO_OSC");
            entity.Property(e => e.TciEffectiveDate).HasColumnName("TCI_EFFECTIVE_DATE");
            entity.Property(e => e.TciFlsaStatusCd)
                .HasMaxLength(1)
                .HasColumnName("TCI_FLSA_STATUS_CD");
            entity.Property(e => e.TciFoc)
                .HasMaxLength(1)
                .HasColumnName("TCI_FOC");
            entity.Property(e => e.TciFocSubcatCd)
                .HasMaxLength(2)
                .HasColumnName("TCI_FOC_SUBCAT_CD");
            entity.Property(e => e.TciFrznTitleDt).HasColumnName("TCI_FRZN_TITLE_DT");
            entity.Property(e => e.TciFrznTitleFlg)
                .HasMaxLength(1)
                .HasColumnName("TCI_FRZN_TITLE_FLG");
            entity.Property(e => e.TciHealthFlag)
                .HasMaxLength(1)
                .HasColumnName("TCI_HEALTH_FLAG");
            entity.Property(e => e.TciLastClsRvDt).HasColumnName("TCI_LAST_CLS_RV_DT");
            entity.Property(e => e.TciLinkgCdTitle)
                .HasMaxLength(3)
                .HasColumnName("TCI_LINKG_CD_TITLE");
            entity.Property(e => e.TciOvrtmExmptCd)
                .HasMaxLength(1)
                .HasColumnName("TCI_OVRTM_EXMPT_CD");
            entity.Property(e => e.TciPayscaleCode)
                .HasMaxLength(1)
                .HasColumnName("TCI_PAYSCALE_CODE");
            entity.Property(e => e.TciPersonlPgmCd)
                .HasMaxLength(1)
                .HasColumnName("TCI_PERSONL_PGM_CD");
            entity.Property(e => e.TciRelatedUnit)
                .HasMaxLength(2)
                .HasColumnName("TCI_RELATED_UNIT");
            entity.Property(e => e.TciRetireCode1)
                .HasMaxLength(1)
                .HasColumnName("TCI_RETIRE_CODE_1");
            entity.Property(e => e.TciRetireCode2)
                .HasMaxLength(1)
                .HasColumnName("TCI_RETIRE_CODE_2");
            entity.Property(e => e.TciRstricTtlFlg)
                .HasMaxLength(1)
                .HasColumnName("TCI_RSTRIC_TTL_FLG");
            entity.Property(e => e.TciSoc)
                .HasMaxLength(3)
                .HasColumnName("TCI_SOC");
            entity.Property(e => e.TciStndHrPerWk)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("TCI_STND_HR_PER_WK");
            entity.Property(e => e.TciSupervisorFlg)
                .HasMaxLength(1)
                .HasColumnName("TCI_SUPERVISOR_FLG");
            entity.Property(e => e.TciTitleAbolDt).HasColumnName("TCI_TITLE_ABOL_DT");
            entity.Property(e => e.TciTitleAbolFlg)
                .HasMaxLength(1)
                .HasColumnName("TCI_TITLE_ABOL_FLG");
            entity.Property(e => e.TciTitleCode)
                .HasMaxLength(4)
                .HasColumnName("TCI_TITLE_CODE");
            entity.Property(e => e.TciTitleName)
                .HasMaxLength(150)
                .HasColumnName("TCI_TITLE_NAME");
            entity.Property(e => e.TciTitleNmAbbrv)
                .HasMaxLength(30)
                .HasColumnName("TCI_TITLE_NM_ABBRV");
            entity.Property(e => e.TciTitleSpclHnd)
                .HasMaxLength(1)
                .HasColumnName("TCI_TITLE_SPCL_HND");
            entity.Property(e => e.TciTitleUnitCd)
                .HasMaxLength(2)
                .HasColumnName("TCI_TITLE_UNIT_CD");
            entity.Property(e => e.TciUcLocation)
                .HasMaxLength(2)
                .HasColumnName("TCI_UC_LOCATION");
            entity.Property(e => e.TciUpdtSource)
                .HasMaxLength(8)
                .HasColumnName("TCI_UPDT_SOURCE");
            entity.Property(e => e.TciUpdtTimestamp)
                .HasMaxLength(26)
                .HasColumnName("TCI_UPDT_TIMESTAMP");
            entity.Property(e => e.TciUseOfTitle)
                .HasMaxLength(1)
                .HasColumnName("TCI_USE_OF_TITLE");
        });

        modelBuilder.Entity<Ctvhme>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CTVHME");

            entity.Property(e => e.HmeAbrvDeptName)
                .HasMaxLength(15)
                .HasColumnName("HME_ABRV_DEPT_NAME");
            entity.Property(e => e.HmeCampusData)
                .HasMaxLength(25)
                .HasColumnName("HME_CAMPUS_DATA");
            entity.Property(e => e.HmeCntlPoint)
                .HasMaxLength(6)
                .HasColumnName("HME_CNTL_POINT");
            entity.Property(e => e.HmeDeptAddress)
                .HasMaxLength(30)
                .HasColumnName("HME_DEPT_ADDRESS");
            entity.Property(e => e.HmeDeptLocCode)
                .HasMaxLength(2)
                .HasColumnName("HME_DEPT_LOC_CODE");
            entity.Property(e => e.HmeDeptMailCode)
                .HasMaxLength(5)
                .HasColumnName("HME_DEPT_MAIL_CODE");
            entity.Property(e => e.HmeDeptName)
                .HasMaxLength(30)
                .HasColumnName("HME_DEPT_NAME");
            entity.Property(e => e.HmeDeptNo)
                .HasMaxLength(6)
                .HasColumnName("HME_DEPT_NO");
            entity.Property(e => e.HmeDeptTypeCd)
                .HasMaxLength(2)
                .HasColumnName("HME_DEPT_TYPE_CD");
            entity.Property(e => e.HmeLastAction)
                .HasMaxLength(1)
                .HasColumnName("HME_LAST_ACTION");
            entity.Property(e => e.HmeLastActionDt).HasColumnName("HME_LAST_ACTION_DT");
            entity.Property(e => e.HmeLayoffUnitCd)
                .HasMaxLength(6)
                .HasColumnName("HME_LAYOFF_UNIT_CD");
            entity.Property(e => e.HmeOnlyAddress)
                .HasMaxLength(1)
                .HasColumnName("HME_ONLY_ADDRESS");
            entity.Property(e => e.HmeOrgUnitCd)
                .HasMaxLength(4)
                .HasColumnName("HME_ORG_UNIT_CD");
            entity.Property(e => e.HmePickupMail)
                .HasMaxLength(1)
                .HasColumnName("HME_PICKUP_MAIL");
            entity.Property(e => e.HmeSubLocation)
                .HasMaxLength(1)
                .HasColumnName("HME_SUB_LOCATION");
            entity.Property(e => e.PrimaryInd)
                .HasMaxLength(1)
                .HasColumnName("PRIMARY_IND");
            entity.Property(e => e.SchAbbrv)
                .HasMaxLength(12)
                .HasColumnName("SCH_ABBRV");
            entity.Property(e => e.UcdDeptName)
                .HasMaxLength(35)
                .HasColumnName("UCD_DEPT_NAME");
            entity.Property(e => e.UcdDivisionDescription)
                .HasMaxLength(25)
                .HasColumnName("UCD_DIVISION_DESCRIPTION");
            entity.Property(e => e.UcdLocationCode)
                .HasMaxLength(1)
                .HasColumnName("UCD_LOCATION_CODE");
            entity.Property(e => e.UcdLocationDescription)
                .HasMaxLength(19)
                .HasColumnName("UCD_LOCATION_DESCRIPTION");
            entity.Property(e => e.UcdSchoolDivision)
                .HasMaxLength(2)
                .HasColumnName("UCD_SCHOOL_DIVISION");
            entity.Property(e => e.UsedInd)
                .HasMaxLength(1)
                .HasColumnName("USED_IND");
        });

        modelBuilder.Entity<DepartmentBudgetEarnFV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DEPARTMENT_BUDGET_EARN_F_V");

            entity.Property(e => e.AcctCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACCT_CD_D_KEY");
            entity.Property(e => e.BusUnitDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BUS_UNIT_D_KEY");
            entity.Property(e => e.DBudgetFSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("D_BUDGET_F__SVM_IS_MOSTRECENT");
            entity.Property(e => e.DBudgetFSvmSeqMrf).HasColumnName("D_BUDGET_F__SVM_SEQ_MRF");
            entity.Property(e => e.DBudgetFSvmSeqNum).HasColumnName("D_BUDGET_F__SVM_SEQ_NUM");
            entity.Property(e => e.DBudgetSvmEffDt).HasColumnName("D_BUDGET_SVM_EFF_DT");
            entity.Property(e => e.DBudgetSvmEndDt).HasColumnName("D_BUDGET_SVM_END_DT");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DepbdgernDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPBDGERN_D_KEY");
            entity.Property(e => e.DepbdgernFBdgtAmt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("DEPBDGERN_F_BDGT_AMT");
            entity.Property(e => e.DepbdgernFBdgtEffSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPBDGERN_F_BDGT_EFF_SEQ_NUM");
            entity.Property(e => e.DepbdgernFBdgtSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPBDGERN_F_BDGT_SEQ_NUM");
            entity.Property(e => e.DepbdgernFCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPBDGERN_F_CNT");
            entity.Property(e => e.DepbdgernFDstrbtnPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("DEPBDGERN_F_DSTRBTN_PCT");
            entity.Property(e => e.DepbdgernFEffortPct)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("DEPBDGERN_F_EFFORT_PCT");
            entity.Property(e => e.DepbdgernFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPBDGERN_F_KEY");
            entity.Property(e => e.DeptBdgtDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_BDGT_D_KEY");
            entity.Property(e => e.DeptBdgtFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_BDGT_F_KEY");
            entity.Property(e => e.DeptDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_D_KEY");
            entity.Property(e => e.DeptbdgtdtDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPTBDGTDT_D_KEY");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EarnCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EARN_CD_D_KEY");
            entity.Property(e => e.EffDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFF_DT_KEY");
            entity.Property(e => e.Fau2DKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FAU2_D_KEY");
            entity.Property(e => e.Fau4DKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FAU4_D_KEY");
            entity.Property(e => e.FausubDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FAUSUB_D_KEY");
            entity.Property(e => e.FsclYrDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FSCL_YR_DT_KEY");
            entity.Property(e => e.FundingEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FUNDING_END_DT_KEY");
            entity.Property(e => e.JobCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_CD_D_KEY");
            entity.Property(e => e.PosnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_D_KEY");
            entity.Property(e => e.PosnPoolDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_POOL_D_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<DepartmentDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DEPARTMENT_D_V");

            entity.HasIndex(e => e.DeptId, "ucpathods_ps_dept_key");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DeptBdgtId)
                .HasMaxLength(10)
                .HasColumnName("DEPT_BDGT_ID");
            entity.Property(e => e.DeptBdgtLvlDesc)
                .HasMaxLength(30)
                .HasColumnName("DEPT_BDGT_LVL_DESC");
            entity.Property(e => e.DeptBdgtLvlInd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_BDGT_LVL_IND");
            entity.Property(e => e.DeptCmpyCd)
                .HasMaxLength(3)
                .HasColumnName("DEPT_CMPY_CD");
            entity.Property(e => e.DeptDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_D_KEY");
            entity.Property(e => e.DeptDesc)
                .HasMaxLength(30)
                .HasColumnName("DEPT_DESC");
            entity.Property(e => e.DeptEffDt).HasColumnName("DEPT_EFF_DT");
            entity.Property(e => e.DeptEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_EFF_STAT_CD");
            entity.Property(e => e.DeptEstabId)
                .HasMaxLength(12)
                .HasColumnName("DEPT_ESTAB_ID");
            entity.Property(e => e.DeptId)
                .HasMaxLength(10)
                .HasColumnName("DEPT_ID");
            entity.Property(e => e.DeptLocShortDesc)
                .HasMaxLength(10)
                .HasColumnName("DEPT_LOC_SHORT_DESC");
            entity.Property(e => e.DeptSetId)
                .HasMaxLength(5)
                .HasColumnName("DEPT_SET_ID");
            entity.Property(e => e.DeptSetIdLocCd)
                .HasMaxLength(5)
                .HasColumnName("DEPT_SET_ID_LOC_CD");
            entity.Property(e => e.DeptShortDesc)
                .HasMaxLength(10)
                .HasColumnName("DEPT_SHORT_DESC");
            entity.Property(e => e.DeptSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("DEPT__SVM_IS_MOSTRECENT");
            entity.Property(e => e.DeptSvmSeqMrf).HasColumnName("DEPT__SVM_SEQ_MRF");
            entity.Property(e => e.DeptSvmSeqNum).HasColumnName("DEPT__SVM_SEQ_NUM");
            entity.Property(e => e.DeptTxLocCd)
                .HasMaxLength(10)
                .HasColumnName("DEPT_TX_LOC_CD");
            entity.Property(e => e.DeptUcLoc3Cd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_UC_LOC3_CD");
            entity.Property(e => e.DeptUcLoc3Desc)
                .HasMaxLength(30)
                .HasColumnName("DEPT_UC_LOC3_DESC");
            entity.Property(e => e.DeptUcRollupId)
                .HasMaxLength(10)
                .HasColumnName("DEPT_UC_ROLLUP_ID");
            entity.Property(e => e.DeptUcSauCd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_UC_SAU_CD");
            entity.Property(e => e.DeptUcSauDesc)
                .HasMaxLength(30)
                .HasColumnName("DEPT_UC_SAU_DESC");
            entity.Property(e => e.DeptUcTypeCd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_UC_TYPE_CD");
            entity.Property(e => e.DeptUcTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("DEPT_UC_TYPE_DESC");
            entity.Property(e => e.DeptUseBdgtsInd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_USE_BDGTS_IND");
            entity.Property(e => e.DeptUseDstrbtnInd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_USE_DSTRBTN_IND");
            entity.Property(e => e.DeptUseEncmbrncsInd)
                .HasMaxLength(1)
                .HasColumnName("DEPT_USE_ENCMBRNCS_IND");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<DiversEthnicityDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DIVERS_ETHNICITY_D_V");

            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EthnctyGrpCd)
                .HasMaxLength(8)
                .HasColumnName("ETHNCTY_GRP_CD");
            entity.Property(e => e.EthnctyPrmyEthnctyFlg)
                .HasMaxLength(1)
                .HasColumnName("ETHNCTY_PRMY_ETHNCTY_FLG");
            entity.Property(e => e.EthnctyRgltnRegnCd)
                .HasMaxLength(5)
                .HasColumnName("ETHNCTY_RGLTN_REGN_CD");
            entity.Property(e => e.EthnctySetId)
                .HasMaxLength(5)
                .HasColumnName("ETHNCTY_SET_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<Dvtloa>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DVTLOA");

            entity.Property(e => e.LoaCode)
                .HasMaxLength(2)
                .HasColumnName("LOA_CODE");
            entity.Property(e => e.LoaDescription)
                .HasMaxLength(40)
                .HasColumnName("LOA_DESCRIPTION");
        });

        modelBuilder.Entity<EarnCodeDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("EARN_CODE_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EarnCd)
                .HasMaxLength(3)
                .HasColumnName("EARN_CD");
            entity.Property(e => e.EarnCdAddDispsblErngsFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_ADD_DISPSBL_ERNGS_FLG");
            entity.Property(e => e.EarnCdAddGrossFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_ADD_GROSS_FLG");
            entity.Property(e => e.EarnCdAllowEmpTypeCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_ALLOW_EMP_TYPE_CD");
            entity.Property(e => e.EarnCdAllowEmpTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_ALLOW_EMP_TYPE_DESC");
            entity.Property(e => e.EarnCdAmtOrHrsDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_AMT_OR_HRS_DESC");
            entity.Property(e => e.EarnCdAmtOrHrsInd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_AMT_OR_HRS_IND");
            entity.Property(e => e.EarnCdBdgtEffectDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_BDGT_EFFECT_DESC");
            entity.Property(e => e.EarnCdBdgtEffectInd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_BDGT_EFFECT_IND");
            entity.Property(e => e.EarnCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EARN_CD_D_KEY");
            entity.Property(e => e.EarnCdDedctnPybckCd)
                .HasMaxLength(6)
                .HasColumnName("EARN_CD_DEDCTN_PYBCK_CD");
            entity.Property(e => e.EarnCdDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_DESC");
            entity.Property(e => e.EarnCdEarnYtdMaxAmt)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("EARN_CD_EARN_YTD_MAX_AMT");
            entity.Property(e => e.EarnCdEffDt).HasColumnName("EARN_CD_EFF_DT");
            entity.Property(e => e.EarnCdEffectOnFlsaCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_EFFECT_ON_FLSA_CD");
            entity.Property(e => e.EarnCdEffectOnFlsaDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_EFFECT_ON_FLSA_DESC");
            entity.Property(e => e.EarnCdElgblForRetropayFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_ELGBL_FOR_RETROPAY_FLG");
            entity.Property(e => e.EarnCdFactorEarnAdjstAmt)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("EARN_CD_FACTOR_EARN_ADJST_AMT");
            entity.Property(e => e.EarnCdFactorHrsAdjstAmt)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("EARN_CD_FACTOR_HRS_ADJST_AMT");
            entity.Property(e => e.EarnCdFactorMultRt)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("EARN_CD_FACTOR_MULT_RT");
            entity.Property(e => e.EarnCdFactorRtAdjstAmt)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("EARN_CD_FACTOR_RT_ADJST_AMT");
            entity.Property(e => e.EarnCdFlatAmt)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("EARN_CD_FLAT_AMT");
            entity.Property(e => e.EarnCdFlsaCtgyCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_FLSA_CTGY_CD");
            entity.Property(e => e.EarnCdFlsaCtgyDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_FLSA_CTGY_DESC");
            entity.Property(e => e.EarnCdFwtFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_FWT_FLG");
            entity.Property(e => e.EarnCdGlExpnsNm)
                .HasMaxLength(35)
                .HasColumnName("EARN_CD_GL_EXPNS_NM");
            entity.Property(e => e.EarnCdHrlyRtMaxAmt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EARN_CD_HRLY_RT_MAX_AMT");
            entity.Property(e => e.EarnCdHrsDstrbtnFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_HRS_DSTRBTN_FLG");
            entity.Property(e => e.EarnCdHrsOnlyFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_HRS_ONLY_FLG");
            entity.Property(e => e.EarnCdInc1042Desc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_INC_1042_DESC");
            entity.Property(e => e.EarnCdInc1042Flg)
                .HasMaxLength(2)
                .HasColumnName("EARN_CD_INC_1042_FLG");
            entity.Property(e => e.EarnCdMaintnBalsFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_MAINTN_BALS_FLG");
            entity.Property(e => e.EarnCdOnAcumltrEarnCd)
                .HasMaxLength(3)
                .HasColumnName("EARN_CD_ON_ACUMLTR_EARN_CD");
            entity.Property(e => e.EarnCdOnEarnCd)
                .HasMaxLength(3)
                .HasColumnName("EARN_CD_ON_EARN_CD");
            entity.Property(e => e.EarnCdOnTypeCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_ON_TYPE_CD");
            entity.Property(e => e.EarnCdOnTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_ON_TYPE_DESC");
            entity.Property(e => e.EarnCdPerunitOvrdRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EARN_CD_PERUNIT_OVRD_RT");
            entity.Property(e => e.EarnCdPnaUseSingleEmpFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_PNA_USE_SINGLE_EMP_FLG");
            entity.Property(e => e.EarnCdPymtTypeCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_PYMT_TYPE_CD");
            entity.Property(e => e.EarnCdPymtTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_PYMT_TYPE_DESC");
            entity.Property(e => e.EarnCdRglrPayIncldFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_RGLR_PAY_INCLD_FLG");
            entity.Property(e => e.EarnCdSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EARN_CD_SEQ_NUM");
            entity.Property(e => e.EarnCdShiftDiffElgblFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SHIFT_DIFF_ELGBL_FLG");
            entity.Property(e => e.EarnCdShortDesc)
                .HasMaxLength(10)
                .HasColumnName("EARN_CD_SHORT_DESC");
            entity.Property(e => e.EarnCdSpclCalctnFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SPCL_CALCTN_FLG");
            entity.Property(e => e.EarnCdStatCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_STAT_CD");
            entity.Property(e => e.EarnCdSubjFicaFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SUBJ_FICA_FLG");
            entity.Property(e => e.EarnCdSubjFutFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SUBJ_FUT_FLG");
            entity.Property(e => e.EarnCdSubjFwtFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SUBJ_FWT_FLG");
            entity.Property(e => e.EarnCdSubjRglrTxRtsFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SUBJ_RGLR_TX_RTS_FLG");
            entity.Property(e => e.EarnCdSubtractEarnsFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_SUBTRACT_EARNS_FLG");
            entity.Property(e => e.EarnCdSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("EARN_CD__SVM_IS_MOSTRECENT");
            entity.Property(e => e.EarnCdSvmSeqMrf).HasColumnName("EARN_CD__SVM_SEQ_MRF");
            entity.Property(e => e.EarnCdSvmSeqNum).HasColumnName("EARN_CD__SVM_SEQ_NUM");
            entity.Property(e => e.EarnCdTipsCtgyCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_TIPS_CTGY_CD");
            entity.Property(e => e.EarnCdTipsCtgyDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_TIPS_CTGY_DESC");
            entity.Property(e => e.EarnCdTxGrossCompntCd)
                .HasMaxLength(5)
                .HasColumnName("EARN_CD_TX_GROSS_COMPNT_CD");
            entity.Property(e => e.EarnCdTxMthdCd)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_TX_MTHD_CD");
            entity.Property(e => e.EarnCdTxMthdDesc)
                .HasMaxLength(30)
                .HasColumnName("EARN_CD_TX_MTHD_DESC");
            entity.Property(e => e.EarnCdUsedToPayRetroFlg)
                .HasMaxLength(1)
                .HasColumnName("EARN_CD_USED_TO_PAY_RETRO_FLG");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<Edbeffrpt>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("edbeffrpt");

            entity.Property(e => e.EdbeffActual)
                .HasColumnType("numeric(13, 4)")
                .HasColumnName("edbeff_actual");
            entity.Property(e => e.EdbeffApptNum).HasColumnName("edbeff_appt_num");
            entity.Property(e => e.EdbeffDistDos)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("edbeff_dist_dos");
            entity.Property(e => e.EdbeffDistNum).HasColumnName("edbeff_dist_num");
            entity.Property(e => e.EdbeffDistPayrate)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("edbeff_dist_payrate");
            entity.Property(e => e.EdbeffDistPercent)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("edbeff_dist_percent");
            entity.Property(e => e.EdbeffDistStep)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("edbeff_dist_step");
            entity.Property(e => e.EdbeffDosAnnualRate)
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("edbeff_dos_annual_rate");
            entity.Property(e => e.EdbeffEmpName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("edbeff_emp_name");
            entity.Property(e => e.EdbeffEmployeeId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("edbeff_employee_id");
            entity.Property(e => e.EdbeffFau)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("edbeff_fau");
            entity.Property(e => e.EdbeffGrossamt)
                .HasColumnType("numeric(13, 4)")
                .HasColumnName("edbeff_grossamt");
            entity.Property(e => e.EdbeffPayBeginDate)
                .HasColumnType("datetime")
                .HasColumnName("edbeff_pay_begin_date");
            entity.Property(e => e.EdbeffPayEndDate)
                .HasColumnType("datetime")
                .HasColumnName("edbeff_pay_end_date");
            entity.Property(e => e.EdbeffSeqNum).HasColumnName("edbeff_seq_num");
            entity.Property(e => e.EdbeffTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("edbeff_title_code");
            entity.Property(e => e.EdbeffUnitDos)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("edbeff_unit_dos");
            entity.Property(e => e.EdbeffUnitPayrate)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("edbeff_unit_payrate");
            entity.Property(e => e.EdbeffUnitPercent)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("edbeff_unit_percent");
            entity.Property(e => e.EdbeffUnitStfAnnual)
                .HasColumnType("numeric(13, 4)")
                .HasColumnName("edbeff_unit_stf_annual");
            entity.Property(e => e.EdbeffUnitStfFte)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("edbeff_unit_stf_fte");
        });

        modelBuilder.Entity<EdbeffrptLog>(entity =>
        {
            entity.HasKey(e => e.EffrptId);

            entity.ToTable("edbeffrpt_log");

            entity.Property(e => e.EffrptId).HasColumnName("effrpt_id");
            entity.Property(e => e.EffrptDldate)
                .HasColumnType("datetime")
                .HasColumnName("effrpt_dldate");
        });

        modelBuilder.Entity<EdbperV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("EDBPER_V");

            entity.Property(e => e.Aasel)
                .HasMaxLength(1)
                .HasColumnName("AASEL");
            entity.Property(e => e.Academic)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC");
            entity.Property(e => e.AcademicFederation)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC_FEDERATION");
            entity.Property(e => e.AcademicSenate)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC_SENATE");
            entity.Property(e => e.AltDeptCd)
                .HasMaxLength(6)
                .HasColumnName("ALT_DEPT_CD");
            entity.Property(e => e.AltHomeDept)
                .HasMaxLength(6)
                .HasColumnName("ALT_HOME_DEPT");
            entity.Property(e => e.BirthDate).HasColumnName("BIRTH_DATE");
            entity.Property(e => e.Confidential)
                .HasMaxLength(1)
                .HasColumnName("CONFIDENTIAL");
            entity.Property(e => e.CreditFromDate).HasColumnName("CREDIT_FROM_DATE");
            entity.Property(e => e.CutCapEligInd)
                .HasMaxLength(1)
                .HasColumnName("CUT_CAP_ELIG_IND");
            entity.Property(e => e.EduLevel)
                .HasMaxLength(1)
                .HasColumnName("EDU_LEVEL");
            entity.Property(e => e.EduLevelYr)
                .HasMaxLength(2)
                .HasColumnName("EDU_LEVEL_YR");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(2)
                .HasColumnName("EMP_CBUC");
            entity.Property(e => e.EmpChangedAt).HasColumnName("EMP_CHANGED_AT");
            entity.Property(e => e.EmpDistUnitCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_DIST_UNIT_CODE");
            entity.Property(e => e.EmpName)
                .HasMaxLength(26)
                .HasColumnName("EMP_NAME");
            entity.Property(e => e.EmpOrgAddrRlse)
                .HasMaxLength(1)
                .HasColumnName("EMP_ORG_ADDR_RLSE");
            entity.Property(e => e.EmpOrgPhoneRlse)
                .HasMaxLength(1)
                .HasColumnName("EMP_ORG_PHONE_RLSE");
            entity.Property(e => e.EmpPriorName)
                .HasMaxLength(26)
                .HasColumnName("EMP_PRIOR_NAME");
            entity.Property(e => e.EmpRelCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_REL_CODE");
            entity.Property(e => e.EmpRelUnit)
                .HasMaxLength(2)
                .HasColumnName("EMP_REL_UNIT");
            entity.Property(e => e.EmpRepCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_REP_CODE");
            entity.Property(e => e.EmpSpecHand)
                .HasMaxLength(1)
                .HasColumnName("EMP_SPEC_HAND");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .HasColumnName("EMP_STATUS");
            entity.Property(e => e.EmpStatusChgDt).HasColumnName("EMP_STATUS_CHG_DT");
            entity.Property(e => e.EmplmtCredit)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("EMPLMT_CREDIT");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(9)
                .HasColumnName("EMPLOYEE_ID");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.HireDate).HasColumnName("HIRE_DATE");
            entity.Property(e => e.HomeAddrRlse)
                .HasMaxLength(1)
                .HasColumnName("HOME_ADDR_RLSE");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(6)
                .HasColumnName("HOME_DEPT");
            entity.Property(e => e.HomePhone)
                .HasMaxLength(10)
                .HasColumnName("HOME_PHONE");
            entity.Property(e => e.HomePhoneRlse)
                .HasMaxLength(1)
                .HasColumnName("HOME_PHONE_RLSE");
            entity.Property(e => e.JobGroupId)
                .HasMaxLength(3)
                .HasColumnName("JOB_GROUP_ID");
            entity.Property(e => e.LadderRank)
                .HasMaxLength(1)
                .HasColumnName("LADDER_RANK");
            entity.Property(e => e.LastAction)
                .HasMaxLength(2)
                .HasColumnName("LAST_ACTION");
            entity.Property(e => e.LastActionDate).HasColumnName("LAST_ACTION_DATE");
            entity.Property(e => e.LastActionOther)
                .HasMaxLength(8)
                .HasColumnName("LAST_ACTION_OTHER");
            entity.Property(e => e.LastChgDate).HasColumnName("LAST_CHG_DATE");
            entity.Property(e => e.LastDayOnPay).HasColumnName("LAST_DAY_ON_PAY");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.LoaBeginDate).HasColumnName("LOA_BEGIN_DATE");
            entity.Property(e => e.LoaReturnDate).HasColumnName("LOA_RETURN_DATE");
            entity.Property(e => e.LoaStatusInd)
                .HasMaxLength(1)
                .HasColumnName("LOA_STATUS_IND");
            entity.Property(e => e.LoaTypeCode)
                .HasMaxLength(2)
                .HasColumnName("LOA_TYPE_CODE");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .HasColumnName("MIDDLE_NAME");
            entity.Property(e => e.Msp)
                .HasMaxLength(1)
                .HasColumnName("MSP");
            entity.Property(e => e.MspCareer)
                .HasMaxLength(1)
                .HasColumnName("MSP_CAREER");
            entity.Property(e => e.MspCareerPartialyr)
                .HasMaxLength(1)
                .HasColumnName("MSP_CAREER_PARTIALYR");
            entity.Property(e => e.MspCasual)
                .HasMaxLength(1)
                .HasColumnName("MSP_CASUAL");
            entity.Property(e => e.MspContract)
                .HasMaxLength(1)
                .HasColumnName("MSP_CONTRACT");
            entity.Property(e => e.MspSeniorMgmt)
                .HasMaxLength(1)
                .HasColumnName("MSP_SENIOR_MGMT");
            entity.Property(e => e.Namesuffix)
                .HasMaxLength(4)
                .HasColumnName("NAMESUFFIX");
            entity.Property(e => e.NetId)
                .HasMaxLength(10)
                .HasColumnName("NET_ID");
            entity.Property(e => e.NextSalaryRev)
                .HasMaxLength(1)
                .HasColumnName("NEXT_SALARY_REV");
            entity.Property(e => e.NextSalrevDate).HasColumnName("NEXT_SALREV_DATE");
            entity.Property(e => e.OathSignDate).HasColumnName("OATH_SIGN_DATE");
            entity.Property(e => e.OrigHireDate).HasColumnName("ORIG_HIRE_DATE");
            entity.Property(e => e.PafGenNum)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("PAF_GEN_NUM");
            entity.Property(e => e.PerCurrStudFlag)
                .HasMaxLength(1)
                .HasColumnName("PER_CURR_STUD_FLAG");
            entity.Property(e => e.PrimaryApptNum)
                .HasMaxLength(2)
                .HasColumnName("PRIMARY_APPT_NUM");
            entity.Property(e => e.PrimaryDistNum)
                .HasMaxLength(2)
                .HasColumnName("PRIMARY_DIST_NUM");
            entity.Property(e => e.PrimaryTitle)
                .HasMaxLength(4)
                .HasColumnName("PRIMARY_TITLE");
            entity.Property(e => e.PriorServiceCode)
                .HasMaxLength(1)
                .HasColumnName("PRIOR_SERVICE_CODE");
            entity.Property(e => e.PriorServiceMths)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("PRIOR_SERVICE_MTHS");
            entity.Property(e => e.ProbEndDate).HasColumnName("PROB_END_DATE");
            entity.Property(e => e.RegisterdUnits)
                .HasColumnType("numeric(3, 1)")
                .HasColumnName("REGISTERD_UNITS");
            entity.Property(e => e.SchoolDivision)
                .HasMaxLength(2)
                .HasColumnName("SCHOOL_DIVISION");
            entity.Property(e => e.SeparateDate).HasColumnName("SEPARATE_DATE");
            entity.Property(e => e.SeparateDestin)
                .HasMaxLength(1)
                .HasColumnName("SEPARATE_DESTIN");
            entity.Property(e => e.SeparateReason)
                .HasMaxLength(2)
                .HasColumnName("SEPARATE_REASON");
            entity.Property(e => e.SklAccrThruDate).HasColumnName("SKL_ACCR_THRU_DATE");
            entity.Property(e => e.SpouseName)
                .HasMaxLength(25)
                .HasColumnName("SPOUSE_NAME");
            entity.Property(e => e.SpouseNameRlse)
                .HasMaxLength(1)
                .HasColumnName("SPOUSE_NAME_RLSE");
            entity.Property(e => e.Ssp)
                .HasMaxLength(1)
                .HasColumnName("SSP");
            entity.Property(e => e.SspCareer)
                .HasMaxLength(1)
                .HasColumnName("SSP_CAREER");
            entity.Property(e => e.SspCareerPartialyr)
                .HasMaxLength(1)
                .HasColumnName("SSP_CAREER_PARTIALYR");
            entity.Property(e => e.SspCasual)
                .HasMaxLength(1)
                .HasColumnName("SSP_CASUAL");
            entity.Property(e => e.SspCasualRestricted)
                .HasMaxLength(1)
                .HasColumnName("SSP_CASUAL_RESTRICTED");
            entity.Property(e => e.SspContract)
                .HasMaxLength(1)
                .HasColumnName("SSP_CONTRACT");
            entity.Property(e => e.SspPerDiem)
                .HasMaxLength(1)
                .HasColumnName("SSP_PER_DIEM");
            entity.Property(e => e.StudentStatus)
                .HasMaxLength(1)
                .HasColumnName("STUDENT_STATUS");
            entity.Property(e => e.Supervisor)
                .HasMaxLength(1)
                .HasColumnName("SUPERVISOR");
            entity.Property(e => e.TeachingFaculty)
                .HasMaxLength(1)
                .HasColumnName("TEACHING_FACULTY");
            entity.Property(e => e.UcdAdcCode)
                .HasMaxLength(1)
                .HasColumnName("UCD_ADC_CODE");
            entity.Property(e => e.UcdAdcDate).HasColumnName("UCD_ADC_DATE");
            entity.Property(e => e.UcdAdminDept)
                .HasMaxLength(6)
                .HasColumnName("UCD_ADMIN_DEPT");
            entity.Property(e => e.UcdEmail)
                .HasMaxLength(60)
                .HasColumnName("UCD_EMAIL");
            entity.Property(e => e.UcdMailid)
                .HasMaxLength(32)
                .HasColumnName("UCD_MAILID");
            entity.Property(e => e.UcdPiEligible)
                .HasMaxLength(1)
                .HasColumnName("UCD_PI_ELIGIBLE");
            entity.Property(e => e.UcdPiSource)
                .HasMaxLength(1)
                .HasColumnName("UCD_PI_SOURCE");
            entity.Property(e => e.UcdWorkDept)
                .HasMaxLength(6)
                .HasColumnName("UCD_WORK_DEPT");
            entity.Property(e => e.Ucdloginid)
                .HasMaxLength(8)
                .HasColumnName("UCDLOGINID");
            entity.Property(e => e.VisaEndDate).HasColumnName("VISA_END_DATE");
            entity.Property(e => e.VisaType)
                .HasMaxLength(2)
                .HasColumnName("VISA_TYPE");
            entity.Property(e => e.WorkLoc)
                .HasMaxLength(1)
                .HasColumnName("WORK_LOC");
            entity.Property(e => e.Wosemp)
                .HasMaxLength(1)
                .HasColumnName("WOSEMP");
        });

        modelBuilder.Entity<EdbperVc>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("EDBPER_VC");

            entity.Property(e => e.Aasel)
                .HasMaxLength(1)
                .HasColumnName("AASEL");
            entity.Property(e => e.Academic)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC");
            entity.Property(e => e.AcademicFederation)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC_FEDERATION");
            entity.Property(e => e.AcademicSenate)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC_SENATE");
            entity.Property(e => e.AdminSchDiv)
                .HasMaxLength(2)
                .HasColumnName("ADMIN_SCH_DIV");
            entity.Property(e => e.AltDeptCd)
                .HasMaxLength(6)
                .HasColumnName("ALT_DEPT_CD");
            entity.Property(e => e.AltHomeDept)
                .HasMaxLength(6)
                .HasColumnName("ALT_HOME_DEPT");
            entity.Property(e => e.BirthDate).HasColumnName("BIRTH_DATE");
            entity.Property(e => e.Confidential)
                .HasMaxLength(1)
                .HasColumnName("CONFIDENTIAL");
            entity.Property(e => e.CreditFromDate).HasColumnName("CREDIT_FROM_DATE");
            entity.Property(e => e.CutCapEligInd)
                .HasMaxLength(1)
                .HasColumnName("CUT_CAP_ELIG_IND");
            entity.Property(e => e.EduLevel)
                .HasMaxLength(1)
                .HasColumnName("EDU_LEVEL");
            entity.Property(e => e.EduLevelYr)
                .HasMaxLength(2)
                .HasColumnName("EDU_LEVEL_YR");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(2)
                .HasColumnName("EMP_CBUC");
            entity.Property(e => e.EmpChangedAt).HasColumnName("EMP_CHANGED_AT");
            entity.Property(e => e.EmpDistUnitCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_DIST_UNIT_CODE");
            entity.Property(e => e.EmpName)
                .HasMaxLength(26)
                .HasColumnName("EMP_NAME");
            entity.Property(e => e.EmpOrgAddrRlse)
                .HasMaxLength(1)
                .HasColumnName("EMP_ORG_ADDR_RLSE");
            entity.Property(e => e.EmpOrgPhoneRlse)
                .HasMaxLength(1)
                .HasColumnName("EMP_ORG_PHONE_RLSE");
            entity.Property(e => e.EmpPriorName)
                .HasMaxLength(26)
                .HasColumnName("EMP_PRIOR_NAME");
            entity.Property(e => e.EmpRelCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_REL_CODE");
            entity.Property(e => e.EmpRelUnit)
                .HasMaxLength(2)
                .HasColumnName("EMP_REL_UNIT");
            entity.Property(e => e.EmpRepCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_REP_CODE");
            entity.Property(e => e.EmpSpecHand)
                .HasMaxLength(1)
                .HasColumnName("EMP_SPEC_HAND");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .HasColumnName("EMP_STATUS");
            entity.Property(e => e.EmpStatusChgDt).HasColumnName("EMP_STATUS_CHG_DT");
            entity.Property(e => e.EmplmtCredit)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("EMPLMT_CREDIT");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(9)
                .HasColumnName("EMPLOYEE_ID");
            entity.Property(e => e.EthnicId)
                .HasMaxLength(1)
                .HasColumnName("ETHNIC_ID");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.GradStudent)
                .HasMaxLength(1)
                .HasColumnName("GRAD_STUDENT");
            entity.Property(e => e.HandicapStat)
                .HasMaxLength(1)
                .HasColumnName("HANDICAP_STAT");
            entity.Property(e => e.HireDate).HasColumnName("HIRE_DATE");
            entity.Property(e => e.HomeAddrRlse)
                .HasMaxLength(1)
                .HasColumnName("HOME_ADDR_RLSE");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(6)
                .HasColumnName("HOME_DEPT");
            entity.Property(e => e.HomePhone)
                .HasMaxLength(10)
                .HasColumnName("HOME_PHONE");
            entity.Property(e => e.HomePhoneRlse)
                .HasMaxLength(1)
                .HasColumnName("HOME_PHONE_RLSE");
            entity.Property(e => e.JobGroupId)
                .HasMaxLength(3)
                .HasColumnName("JOB_GROUP_ID");
            entity.Property(e => e.LadderRank)
                .HasMaxLength(1)
                .HasColumnName("LADDER_RANK");
            entity.Property(e => e.LastAction)
                .HasMaxLength(2)
                .HasColumnName("LAST_ACTION");
            entity.Property(e => e.LastActionDate).HasColumnName("LAST_ACTION_DATE");
            entity.Property(e => e.LastActionOther)
                .HasMaxLength(8)
                .HasColumnName("LAST_ACTION_OTHER");
            entity.Property(e => e.LastChgDate).HasColumnName("LAST_CHG_DATE");
            entity.Property(e => e.LastDayOnPay).HasColumnName("LAST_DAY_ON_PAY");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.LoaBeginDate).HasColumnName("LOA_BEGIN_DATE");
            entity.Property(e => e.LoaReturnDate).HasColumnName("LOA_RETURN_DATE");
            entity.Property(e => e.LoaStatusInd)
                .HasMaxLength(1)
                .HasColumnName("LOA_STATUS_IND");
            entity.Property(e => e.LoaTypeCode)
                .HasMaxLength(2)
                .HasColumnName("LOA_TYPE_CODE");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .HasColumnName("MIDDLE_NAME");
            entity.Property(e => e.Msp)
                .HasMaxLength(1)
                .HasColumnName("MSP");
            entity.Property(e => e.MspCareer)
                .HasMaxLength(1)
                .HasColumnName("MSP_CAREER");
            entity.Property(e => e.MspCareerPartialyr)
                .HasMaxLength(1)
                .HasColumnName("MSP_CAREER_PARTIALYR");
            entity.Property(e => e.MspCasual)
                .HasMaxLength(1)
                .HasColumnName("MSP_CASUAL");
            entity.Property(e => e.MspContract)
                .HasMaxLength(1)
                .HasColumnName("MSP_CONTRACT");
            entity.Property(e => e.MspSeniorMgmt)
                .HasMaxLength(1)
                .HasColumnName("MSP_SENIOR_MGMT");
            entity.Property(e => e.Namesuffix)
                .HasMaxLength(4)
                .HasColumnName("NAMESUFFIX");
            entity.Property(e => e.NetId)
                .HasMaxLength(10)
                .HasColumnName("NET_ID");
            entity.Property(e => e.NextSalaryRev)
                .HasMaxLength(1)
                .HasColumnName("NEXT_SALARY_REV");
            entity.Property(e => e.NextSalrevDate).HasColumnName("NEXT_SALREV_DATE");
            entity.Property(e => e.OathSignDate).HasColumnName("OATH_SIGN_DATE");
            entity.Property(e => e.OrigHireDate).HasColumnName("ORIG_HIRE_DATE");
            entity.Property(e => e.PerCurrStudFlag)
                .HasMaxLength(1)
                .HasColumnName("PER_CURR_STUD_FLAG");
            entity.Property(e => e.PrimaryApptNum)
                .HasMaxLength(2)
                .HasColumnName("PRIMARY_APPT_NUM");
            entity.Property(e => e.PrimaryDistNum)
                .HasMaxLength(2)
                .HasColumnName("PRIMARY_DIST_NUM");
            entity.Property(e => e.PrimaryTitle)
                .HasMaxLength(4)
                .HasColumnName("PRIMARY_TITLE");
            entity.Property(e => e.PriorServiceCode)
                .HasMaxLength(1)
                .HasColumnName("PRIOR_SERVICE_CODE");
            entity.Property(e => e.PriorServiceMths)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("PRIOR_SERVICE_MTHS");
            entity.Property(e => e.ProbEndDate).HasColumnName("PROB_END_DATE");
            entity.Property(e => e.RegisterdUnits)
                .HasColumnType("numeric(3, 1)")
                .HasColumnName("REGISTERD_UNITS");
            entity.Property(e => e.SchoolDivision)
                .HasMaxLength(2)
                .HasColumnName("SCHOOL_DIVISION");
            entity.Property(e => e.SeparateDate).HasColumnName("SEPARATE_DATE");
            entity.Property(e => e.SeparateDestin)
                .HasMaxLength(1)
                .HasColumnName("SEPARATE_DESTIN");
            entity.Property(e => e.SeparateReason)
                .HasMaxLength(2)
                .HasColumnName("SEPARATE_REASON");
            entity.Property(e => e.Sexcode)
                .HasMaxLength(1)
                .HasColumnName("SEXCODE");
            entity.Property(e => e.SklAccrThruDate).HasColumnName("SKL_ACCR_THRU_DATE");
            entity.Property(e => e.SpouseName)
                .HasMaxLength(25)
                .HasColumnName("SPOUSE_NAME");
            entity.Property(e => e.SpouseNameRlse)
                .HasMaxLength(1)
                .HasColumnName("SPOUSE_NAME_RLSE");
            entity.Property(e => e.Ssp)
                .HasMaxLength(1)
                .HasColumnName("SSP");
            entity.Property(e => e.SspCareer)
                .HasMaxLength(1)
                .HasColumnName("SSP_CAREER");
            entity.Property(e => e.SspCareerPartialyr)
                .HasMaxLength(1)
                .HasColumnName("SSP_CAREER_PARTIALYR");
            entity.Property(e => e.SspCasual)
                .HasMaxLength(1)
                .HasColumnName("SSP_CASUAL");
            entity.Property(e => e.SspCasualRestricted)
                .HasMaxLength(1)
                .HasColumnName("SSP_CASUAL_RESTRICTED");
            entity.Property(e => e.SspContract)
                .HasMaxLength(1)
                .HasColumnName("SSP_CONTRACT");
            entity.Property(e => e.SspPerDiem)
                .HasMaxLength(1)
                .HasColumnName("SSP_PER_DIEM");
            entity.Property(e => e.StudentStatus)
                .HasMaxLength(1)
                .HasColumnName("STUDENT_STATUS");
            entity.Property(e => e.Supervisor)
                .HasMaxLength(1)
                .HasColumnName("SUPERVISOR");
            entity.Property(e => e.TeachingFaculty)
                .HasMaxLength(1)
                .HasColumnName("TEACHING_FACULTY");
            entity.Property(e => e.UcdAdcCode)
                .HasMaxLength(1)
                .HasColumnName("UCD_ADC_CODE");
            entity.Property(e => e.UcdAdcDate).HasColumnName("UCD_ADC_DATE");
            entity.Property(e => e.UcdAdminDept)
                .HasMaxLength(6)
                .HasColumnName("UCD_ADMIN_DEPT");
            entity.Property(e => e.UcdEmail)
                .HasMaxLength(60)
                .HasColumnName("UCD_EMAIL");
            entity.Property(e => e.UcdMailid)
                .HasMaxLength(32)
                .HasColumnName("UCD_MAILID");
            entity.Property(e => e.UcdPiEligible)
                .HasMaxLength(1)
                .HasColumnName("UCD_PI_ELIGIBLE");
            entity.Property(e => e.UcdPiSource)
                .HasMaxLength(1)
                .HasColumnName("UCD_PI_SOURCE");
            entity.Property(e => e.UcdWorkDept)
                .HasMaxLength(6)
                .HasColumnName("UCD_WORK_DEPT");
            entity.Property(e => e.UcdWorkPhone)
                .HasMaxLength(13)
                .HasColumnName("UCD_WORK_PHONE");
            entity.Property(e => e.Ucdloginid)
                .HasMaxLength(8)
                .HasColumnName("UCDLOGINID");
            entity.Property(e => e.VetDisabStat)
                .HasMaxLength(1)
                .HasColumnName("VET_DISAB_STAT");
            entity.Property(e => e.VetStatus)
                .HasMaxLength(1)
                .HasColumnName("VET_STATUS");
            entity.Property(e => e.VisaEndDate).HasColumnName("VISA_END_DATE");
            entity.Property(e => e.WorkLoc)
                .HasMaxLength(1)
                .HasColumnName("WORK_LOC");
            entity.Property(e => e.WorkSchDiv)
                .HasMaxLength(2)
                .HasColumnName("WORK_SCH_DIV");
            entity.Property(e => e.Wosemp)
                .HasMaxLength(1)
                .HasColumnName("WOSEMP");
        });

        modelBuilder.Entity<EmployeeDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("EMPLOYEE_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.EmpBirthDt).HasColumnName("EMP_BIRTH_DT");
            entity.Property(e => e.EmpCtznshpCntryCd)
                .HasMaxLength(3)
                .HasColumnName("EMP_CTZNSHP_CNTRY_CD");
            entity.Property(e => e.EmpCtznshpCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_CNTRY_DESC");
            entity.Property(e => e.EmpCtznshpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_DESC");
            entity.Property(e => e.EmpCtznshpStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_CTZNSHP_STAT_CD");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpEffDt).HasColumnName("EMP_EFF_DT");
            entity.Property(e => e.EmpEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_EFF_STAT_CD");
            entity.Property(e => e.EmpExprDt).HasColumnName("EMP_EXPR_DT");
            entity.Property(e => e.EmpFullTimeStdntCd)
                .HasMaxLength(2)
                .HasColumnName("EMP_FULL_TIME_STDNT_CD");
            entity.Property(e => e.EmpHighEduLvlCd)
                .HasMaxLength(2)
                .HasColumnName("EMP_HIGH_EDU_LVL_CD");
            entity.Property(e => e.EmpHighEduLvlDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_HIGH_EDU_LVL_DESC");
            entity.Property(e => e.EmpHmAddr1Txt)
                .HasMaxLength(55)
                .HasColumnName("EMP_HM_ADDR1_TXT");
            entity.Property(e => e.EmpHmAddr2Txt)
                .HasMaxLength(55)
                .HasColumnName("EMP_HM_ADDR2_TXT");
            entity.Property(e => e.EmpHmCityNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_HM_CITY_NM");
            entity.Property(e => e.EmpHmCntryCd)
                .HasMaxLength(3)
                .HasColumnName("EMP_HM_CNTRY_CD");
            entity.Property(e => e.EmpHmPstlCd)
                .HasMaxLength(12)
                .HasColumnName("EMP_HM_PSTL_CD");
            entity.Property(e => e.EmpHmStCd)
                .HasMaxLength(6)
                .HasColumnName("EMP_HM_ST_CD");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpOrigHireDt).HasColumnName("EMP_ORIG_HIRE_DT");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("EMP_PPS_UID");
            entity.Property(e => e.EmpPrmyEthnctyGrpCd)
                .HasMaxLength(8)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_CD");
            entity.Property(e => e.EmpPrmyEthnctyGrpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_DESC");
            entity.Property(e => e.EmpPrmyFirstNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_FIRST_NM");
            entity.Property(e => e.EmpPrmyFullNm)
                .HasMaxLength(50)
                .HasColumnName("EMP_PRMY_FULL_NM");
            entity.Property(e => e.EmpPrmyLastNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_LAST_NM");
            entity.Property(e => e.EmpPrmyMidNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_MID_NM");
            entity.Property(e => e.EmpPrmyNmSufxCd)
                .HasMaxLength(15)
                .HasColumnName("EMP_PRMY_NM_SUFX_CD");
            entity.Property(e => e.EmpSexCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_SEX_CD");
            entity.Property(e => e.EmpSexDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_SEX_DESC");
            entity.Property(e => e.EmpSpouseFullNm)
                .HasMaxLength(50)
                .HasColumnName("EMP_SPOUSE_FULL_NM");
            entity.Property(e => e.EmpSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("EMP__SVM_IS_MOSTRECENT");
            entity.Property(e => e.EmpSvmSeqMrf).HasColumnName("EMP__SVM_SEQ_MRF");
            entity.Property(e => e.EmpSvmSeqNum).HasColumnName("EMP__SVM_SEQ_NUM");
            entity.Property(e => e.EmpUcAddrReleaseFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_UC_ADDR_RELEASE_FLG");
            entity.Property(e => e.EmpUcPhReleaseFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_UC_PH_RELEASE_FLG");
            entity.Property(e => e.EmpUsWrkElgbltyCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_US_WRK_ELGBLTY_CD");
            entity.Property(e => e.EmpUsWrkElgbltyDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_US_WRK_ELGBLTY_DESC");
            entity.Property(e => e.EmpWrkAddr1Txt)
                .HasMaxLength(55)
                .HasColumnName("EMP_WRK_ADDR1_TXT");
            entity.Property(e => e.EmpWrkAddr2Txt)
                .HasMaxLength(55)
                .HasColumnName("EMP_WRK_ADDR2_TXT");
            entity.Property(e => e.EmpWrkCityNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_WRK_CITY_NM");
            entity.Property(e => e.EmpWrkCntryCd)
                .HasMaxLength(3)
                .HasColumnName("EMP_WRK_CNTRY_CD");
            entity.Property(e => e.EmpWrkCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_WRK_CNTRY_DESC");
            entity.Property(e => e.EmpWrkEmailAddrTxt)
                .HasMaxLength(70)
                .HasColumnName("EMP_WRK_EMAIL_ADDR_TXT");
            entity.Property(e => e.EmpWrkPhNum)
                .HasMaxLength(24)
                .HasColumnName("EMP_WRK_PH_NUM");
            entity.Property(e => e.EmpWrkPstlCd)
                .HasMaxLength(12)
                .HasColumnName("EMP_WRK_PSTL_CD");
            entity.Property(e => e.EmpWrkStCd)
                .HasMaxLength(6)
                .HasColumnName("EMP_WRK_ST_CD");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<EmployeeHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("employeeHistory");

            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("emp_id");
            entity.Property(e => e.EmpName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("emp_name");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("emp_pps_uid");
            entity.Property(e => e.FirstEffDt)
                .HasColumnType("datetime")
                .HasColumnName("first_eff_dt");
            entity.Property(e => e.FirstExprDate)
                .HasColumnType("datetime")
                .HasColumnName("first_expr_date");
            entity.Property(e => e.FirstRole)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("first_role");
            entity.Property(e => e.FirstSeen)
                .HasColumnType("datetime")
                .HasColumnName("first_seen");
            entity.Property(e => e.LastEffDt)
                .HasColumnType("datetime")
                .HasColumnName("last_eff_dt");
            entity.Property(e => e.LastExprDt)
                .HasColumnType("datetime")
                .HasColumnName("last_expr_dt");
            entity.Property(e => e.LastRole)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("last_role");
            entity.Property(e => e.LastSeen)
                .HasColumnType("datetime")
                .HasColumnName("last_seen");
            entity.Property(e => e.LastStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("last_status");
        });

        modelBuilder.Entity<EthnicityGender>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ethnicityGender");

            entity.Property(e => e.EthsexApptDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ethsex_apptDept");
            entity.Property(e => e.EthsexApptDeptName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_apptDeptName");
            entity.Property(e => e.EthsexEthnicity)
                .IsUnicode(false)
                .HasColumnName("ethsex_ethnicity");
            entity.Property(e => e.EthsexExtractDate)
                .HasColumnType("datetime")
                .HasColumnName("ethsex_extractDate");
            entity.Property(e => e.EthsexGender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("ethsex_gender");
            entity.Property(e => e.EthsexHireDate)
                .HasColumnType("datetime")
                .HasColumnName("ethsex_hireDate");
            entity.Property(e => e.EthsexHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ethsex_homeDept");
            entity.Property(e => e.EthsexHomeDeptName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_homeDeptName");
            entity.Property(e => e.EthsexName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_name");
            entity.Property(e => e.EthsexPayRate)
                .HasColumnType("numeric(12, 4)")
                .HasColumnName("ethsex_payRate");
            entity.Property(e => e.EthsexStep)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("ethsex_step");
            entity.Property(e => e.EthsexTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_title");
            entity.Property(e => e.EthsexTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ethsex_titleCode");
        });

        modelBuilder.Entity<EthnicityGender20210201>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ethnicityGender_2021_02_01");

            entity.Property(e => e.EthsexApptDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ethsex_apptDept");
            entity.Property(e => e.EthsexApptDeptName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_apptDeptName");
            entity.Property(e => e.EthsexEthnicity)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("ethsex_ethnicity");
            entity.Property(e => e.EthsexExtractDate)
                .HasColumnType("datetime")
                .HasColumnName("ethsex_extractDate");
            entity.Property(e => e.EthsexGender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("ethsex_gender");
            entity.Property(e => e.EthsexHireDate)
                .HasColumnType("datetime")
                .HasColumnName("ethsex_hireDate");
            entity.Property(e => e.EthsexHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ethsex_homeDept");
            entity.Property(e => e.EthsexHomeDeptName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_homeDeptName");
            entity.Property(e => e.EthsexName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_name");
            entity.Property(e => e.EthsexPayRate)
                .HasColumnType("numeric(12, 4)")
                .HasColumnName("ethsex_payRate");
            entity.Property(e => e.EthsexStep)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("ethsex_step");
            entity.Property(e => e.EthsexTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ethsex_title");
            entity.Property(e => e.EthsexTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ethsex_titleCode");
        });

        modelBuilder.Entity<Export>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("export");

            entity.Property(e => e.AcctCd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ACCT_CD");
            entity.Property(e => e.AddlPayFrequency)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ADDL_PAY_FREQUENCY");
            entity.Property(e => e.AddlPayShift)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ADDL_PAY_SHIFT");
            entity.Property(e => e.AddlSeq)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ADDL_SEQ");
            entity.Property(e => e.AddlpayReason)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ADDLPAY_REASON");
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.CompRatecd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("COMP_RATECD");
            entity.Property(e => e.CrBtDtm)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DedSubsetGenl)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DED_SUBSET_GENL");
            entity.Property(e => e.DedSubsetId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DED_SUBSET_ID");
            entity.Property(e => e.DedTaken)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DED_TAKEN");
            entity.Property(e => e.DedTakenGenl)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DED_TAKEN_GENL");
            entity.Property(e => e.Deptid)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DisableDirDep)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DISABLE_DIR_DEP");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DML_IND");
            entity.Property(e => e.EarningsEndDt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EARNINGS_END_DT");
            entity.Property(e => e.Effdt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EFFDT");
            entity.Property(e => e.EmplRcd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Erncd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ERNCD");
            entity.Property(e => e.GlPayType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GL_PAY_TYPE");
            entity.Property(e => e.GoalAmt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GOAL_AMT");
            entity.Property(e => e.GoalBal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GOAL_BAL");
            entity.Property(e => e.HourlyRt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("HOURLY_RT");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.Locality)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LOCALITY");
            entity.Property(e => e.OdsVrsnNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OkToPay)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("OK_TO_PAY");
            entity.Property(e => e.OthHrs)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("OTH_HRS");
            entity.Property(e => e.OthPay)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("OTH_PAY");
            entity.Property(e => e.PayPeriod1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAY_PERIOD1");
            entity.Property(e => e.PayPeriod2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAY_PERIOD2");
            entity.Property(e => e.PayPeriod3)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAY_PERIOD3");
            entity.Property(e => e.PayPeriod4)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAY_PERIOD4");
            entity.Property(e => e.PayPeriod5)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAY_PERIOD5");
            entity.Property(e => e.PlanType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PLAN_TYPE");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.ProrateAddlPay)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRORATE_ADDL_PAY");
            entity.Property(e => e.ProrateCuiWeeks)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRORATE_CUI_WEEKS");
            entity.Property(e => e.RecordSource)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RECORD_SOURCE");
            entity.Property(e => e.Sepchk)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SEPCHK");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STATE");
            entity.Property(e => e.TaxMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TAX_METHOD");
            entity.Property(e => e.TaxPeriods)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TAX_PERIODS");
            entity.Property(e => e.UpdBtDtm)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<FurloughTarget>(entity =>
        {
            entity.HasKey(e => e.FurloughId).HasName("PK_furlough_target");

            entity.ToTable("furloughTarget");

            entity.Property(e => e.FurloughId).HasColumnName("furlough_ID");
            entity.Property(e => e.FurloughAcct)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("furlough_acct");
            entity.Property(e => e.FurloughBeginDate)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("furlough_begin_date");
            entity.Property(e => e.FurloughChart)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("furlough_chart");
            entity.Property(e => e.FurloughCommittedFySalary)
                .HasDefaultValueSql("((0.0))")
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("furlough_committed_FY_salary");
            entity.Property(e => e.FurloughCommittedNfySalary)
                .HasDefaultValueSql("((0.0))")
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("furlough_committed_NFY_salary");
            entity.Property(e => e.FurloughCumulativeSalary)
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("furlough_cumulative_salary");
            entity.Property(e => e.FurloughCurrentSalary)
                .HasDefaultValueSql("((0.0))")
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("furlough_current_salary");
            entity.Property(e => e.FurloughDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("furlough_dept_code");
            entity.Property(e => e.FurloughDeptName)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("furlough_dept_name");
            entity.Property(e => e.FurloughDos)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("furlough_DOS");
            entity.Property(e => e.FurloughEmpName)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("furlough_emp_name");
            entity.Property(e => e.FurloughEmployeeId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("furlough_employee_id");
            entity.Property(e => e.FurloughEndDate)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("furlough_end_date");
            entity.Property(e => e.FurloughObjConsol)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("furlough_objConsol");
            entity.Property(e => e.FurloughObject)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("furlough_object");
            entity.Property(e => e.FurloughOpFund)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("furlough_OP_fund");
            entity.Property(e => e.FurloughProject)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("furlough_project");
            entity.Property(e => e.FurloughProjectedFySalary)
                .HasComputedColumnSql("(([furlough_cumulative_salary]+[furlough_current_salary])+[furlough_committed_FY_salary])", false)
                .HasColumnType("numeric(12, 4)")
                .HasColumnName("furlough_projected_FY_salary");
            entity.Property(e => e.FurloughProjectedSalary)
                .HasComputedColumnSql("((([furlough_cumulative_salary]+[furlough_current_salary])+[furlough_committed_FY_salary])+[furlough_committed_NFY_salary])", false)
                .HasColumnType("numeric(13, 4)")
                .HasColumnName("furlough_projected_salary");
            entity.Property(e => e.FurloughSubAcct)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("furlough_subAcct");
            entity.Property(e => e.FurloughSubObj)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("furlough_subObj");
            entity.Property(e => e.FurloughTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("furlough_title_code");
        });

        modelBuilder.Entity<GoAnywhereLog>(entity =>
        {
            entity.HasKey(e => e.RecordId);

            entity.ToTable("goAnywhereLog");

            entity.Property(e => e.RecordId).HasColumnName("recordId");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.FileName)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("fileName");
            entity.Property(e => e.Message)
                .IsUnicode(false)
                .HasColumnName("message");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.User)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("user");
        });

        modelBuilder.Entity<HistServiceCredit>(entity =>
        {
            entity.HasKey(e => e.HistServiceCreditRecordId);

            entity.ToTable("hist_serviceCredit");

            entity.HasIndex(e => e.HistServiceCreditMonthOffset, "attainedDate");

            entity.HasIndex(e => new { e.HistServiceCreditHomeDept, e.HistServiceCreditMonthOffset }, "dept_offset");

            entity.HasIndex(e => e.HistServiceCreditHomeDept, "home_dept");

            entity.HasIndex(e => e.HistServiceCreditRecordId, "recordID");

            entity.HasIndex(e => e.HistServiceCreditSchoolId, "schoolID");

            entity.HasIndex(e => new { e.HistServiceCreditSchoolId, e.HistServiceCreditMonthOffset }, "school_offset");

            entity.Property(e => e.HistServiceCreditRecordId).HasColumnName("hist_serviceCredit_record_ID");
            entity.Property(e => e.HistServiceCreditAccrualDate)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_accrual_date");
            entity.Property(e => e.HistServiceCreditCaoDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_cao_display_name");
            entity.Property(e => e.HistServiceCreditCaoMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_cao_mail_ID");
            entity.Property(e => e.HistServiceCreditCaoMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_cao_mothra_ID");
            entity.Property(e => e.HistServiceCreditCurrentAcrucode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_current_acrucode");
            entity.Property(e => e.HistServiceCreditDeptName)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_dept_name");
            entity.Property(e => e.HistServiceCreditEmpName)
                .HasMaxLength(26)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_emp_name");
            entity.Property(e => e.HistServiceCreditEmployeeId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_employee_ID");
            entity.Property(e => e.HistServiceCreditHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_home_dept");
            entity.Property(e => e.HistServiceCreditLastRunDate)
                .HasColumnType("datetime")
                .HasColumnName("hist_serviceCredit_last_run_date");
            entity.Property(e => e.HistServiceCreditMonthOffset).HasColumnName("hist_serviceCredit_month_offset");
            entity.Property(e => e.HistServiceCreditNextAcrucode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_next_acrucode");
            entity.Property(e => e.HistServiceCreditOriginalHireDate)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_original_hire_date");
            entity.Property(e => e.HistServiceCreditProgram)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_program");
            entity.Property(e => e.HistServiceCreditSchoolId)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_school_ID");
            entity.Property(e => e.HistServiceCreditServiceMonths).HasColumnName("hist_serviceCredit_service_months");
            entity.Property(e => e.HistServiceCreditServiceYears).HasColumnName("hist_serviceCredit_service_years");
            entity.Property(e => e.HistServiceCreditTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("hist_serviceCredit_title");
        });

        modelBuilder.Entity<HistappdisV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("HISTAPPDIS_V");

            entity.Property(e => e.AcademicBasis)
                .HasMaxLength(2)
                .HasColumnName("ACADEMIC_BASIS");
            entity.Property(e => e.ActionCode)
                .HasMaxLength(2)
                .HasColumnName("ACTION_CODE");
            entity.Property(e => e.AddDate).HasColumnName("ADD_DATE");
            entity.Property(e => e.ApptBeginDate).HasColumnName("APPT_BEGIN_DATE");
            entity.Property(e => e.ApptDept)
                .HasMaxLength(6)
                .HasColumnName("APPT_DEPT");
            entity.Property(e => e.ApptDuration)
                .HasMaxLength(1)
                .HasColumnName("APPT_DURATION");
            entity.Property(e => e.ApptEndDate).HasColumnName("APPT_END_DATE");
            entity.Property(e => e.ApptFlsaInd)
                .HasMaxLength(1)
                .HasColumnName("APPT_FLSA_IND");
            entity.Property(e => e.ApptNum)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("APPT_NUM");
            entity.Property(e => e.ApptOffScale)
                .HasMaxLength(1)
                .HasColumnName("APPT_OFF_SCALE");
            entity.Property(e => e.ApptPaidOver)
                .HasMaxLength(2)
                .HasColumnName("APPT_PAID_OVER");
            entity.Property(e => e.ApptRepCode)
                .HasMaxLength(1)
                .HasColumnName("APPT_REP_CODE");
            entity.Property(e => e.ApptSpclHndlg)
                .HasMaxLength(1)
                .HasColumnName("APPT_SPCL_HNDLG");
            entity.Property(e => e.ApptType)
                .HasMaxLength(1)
                .HasColumnName("APPT_TYPE");
            entity.Property(e => e.ApptWosInd)
                .HasMaxLength(1)
                .HasColumnName("APPT_WOS_IND");
            entity.Property(e => e.Comments)
                .HasMaxLength(4000)
                .HasColumnName("COMMENTS");
            entity.Property(e => e.DistDeptCode)
                .HasMaxLength(6)
                .HasColumnName("DIST_DEPT_CODE");
            entity.Property(e => e.DistDos)
                .HasMaxLength(3)
                .HasColumnName("DIST_DOS");
            entity.Property(e => e.DistFte)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("DIST_FTE");
            entity.Property(e => e.DistNum)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("DIST_NUM");
            entity.Property(e => e.DistOffAbove)
                .HasMaxLength(1)
                .HasColumnName("DIST_OFF_ABOVE");
            entity.Property(e => e.DistPayrate)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("DIST_PAYRATE");
            entity.Property(e => e.DistPercent)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("DIST_PERCENT");
            entity.Property(e => e.DistPerq)
                .HasMaxLength(3)
                .HasColumnName("DIST_PERQ");
            entity.Property(e => e.DistStep)
                .HasMaxLength(4)
                .HasColumnName("DIST_STEP");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(9)
                .HasColumnName("EMPLOYEE_ID");
            entity.Property(e => e.FauAcct)
                .HasMaxLength(7)
                .HasColumnName("FAU_ACCT");
            entity.Property(e => e.FauChart)
                .HasMaxLength(1)
                .HasColumnName("FAU_CHART");
            entity.Property(e => e.FauObject)
                .HasMaxLength(4)
                .HasColumnName("FAU_OBJECT");
            entity.Property(e => e.FauOpFund)
                .HasMaxLength(6)
                .HasColumnName("FAU_OP_FUND");
            entity.Property(e => e.FauOrgCd)
                .HasMaxLength(4)
                .HasColumnName("FAU_ORG_CD");
            entity.Property(e => e.FauProject)
                .HasMaxLength(10)
                .HasColumnName("FAU_PROJECT");
            entity.Property(e => e.FauSubFundGroupCd)
                .HasMaxLength(6)
                .HasColumnName("FAU_SUB_FUND_GROUP_CD");
            entity.Property(e => e.FauSubFundGrpTypCd)
                .HasMaxLength(2)
                .HasColumnName("FAU_SUB_FUND_GRP_TYP_CD");
            entity.Property(e => e.FauSubacct)
                .HasMaxLength(5)
                .HasColumnName("FAU_SUBACCT");
            entity.Property(e => e.FauSubobj)
                .HasMaxLength(3)
                .HasColumnName("FAU_SUBOBJ");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.FixedVarCode)
                .HasMaxLength(1)
                .HasColumnName("FIXED_VAR_CODE");
            entity.Property(e => e.Grade)
                .HasMaxLength(2)
                .HasColumnName("GRADE");
            entity.Property(e => e.HireDate).HasColumnName("HIRE_DATE");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(6)
                .HasColumnName("HOME_DEPT");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.LeaveAcrucode)
                .HasMaxLength(1)
                .HasColumnName("LEAVE_ACRUCODE");
            entity.Property(e => e.PayBeginDate).HasColumnName("PAY_BEGIN_DATE");
            entity.Property(e => e.PayEndDate).HasColumnName("PAY_END_DATE");
            entity.Property(e => e.PayRate)
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("PAY_RATE");
            entity.Property(e => e.PaySchedule)
                .HasMaxLength(2)
                .HasColumnName("PAY_SCHEDULE");
            entity.Property(e => e.PercentFulltime)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("PERCENT_FULLTIME");
            entity.Property(e => e.PersonnelPgm)
                .HasMaxLength(1)
                .HasColumnName("PERSONNEL_PGM");
            entity.Property(e => e.RangeAdjDuc)
                .HasMaxLength(1)
                .HasColumnName("RANGE_ADJ_DUC");
            entity.Property(e => e.RateCode)
                .HasMaxLength(1)
                .HasColumnName("RATE_CODE");
            entity.Property(e => e.RetirementCode)
                .HasMaxLength(1)
                .HasColumnName("RETIREMENT_CODE");
            entity.Property(e => e.TimeReptCode)
                .HasMaxLength(1)
                .HasColumnName("TIME_REPT_CODE");
            entity.Property(e => e.TitleCode)
                .HasMaxLength(4)
                .HasColumnName("TITLE_CODE");
            entity.Property(e => e.TitleUnitCode)
                .HasMaxLength(2)
                .HasColumnName("TITLE_UNIT_CODE");
            entity.Property(e => e.UcdDistAdcCode)
                .HasMaxLength(1)
                .HasColumnName("UCD_DIST_ADC_CODE");
            entity.Property(e => e.WorkStudyPgm)
                .HasMaxLength(1)
                .HasColumnName("WORK_STUDY_PGM");
        });

        modelBuilder.Entity<HistloaV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("HISTLOA_V");

            entity.Property(e => e.Academic)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC");
            entity.Property(e => e.ApptType)
                .HasMaxLength(1)
                .HasColumnName("APPT_TYPE");
            entity.Property(e => e.EmpName)
                .HasMaxLength(26)
                .HasColumnName("EMP_NAME");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .HasColumnName("EMP_STATUS");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(9)
                .HasColumnName("EMPLOYEE_ID");
            entity.Property(e => e.HmeDeptName)
                .HasMaxLength(30)
                .HasColumnName("HME_DEPT_NAME");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(6)
                .HasColumnName("HOME_DEPT");
            entity.Property(e => e.LoaBeginDate).HasColumnName("LOA_BEGIN_DATE");
            entity.Property(e => e.LoaReturnDate).HasColumnName("LOA_RETURN_DATE");
            entity.Property(e => e.LoaStatusInd)
                .HasMaxLength(1)
                .HasColumnName("LOA_STATUS_IND");
            entity.Property(e => e.LoaTypeCode)
                .HasMaxLength(2)
                .HasColumnName("LOA_TYPE_CODE");
            entity.Property(e => e.PersonalPgmCd)
                .HasMaxLength(1)
                .HasColumnName("PERSONAL_PGM_CD");
            entity.Property(e => e.RepCode)
                .HasMaxLength(1)
                .HasColumnName("REP_CODE");
            entity.Property(e => e.TitleCode)
                .HasMaxLength(4)
                .HasColumnName("TITLE_CODE");
            entity.Property(e => e.TitleUnitCode)
                .HasMaxLength(2)
                .HasColumnName("TITLE_UNIT_CODE");
            entity.Property(e => e.UcdSchoolDivision)
                .HasMaxLength(2)
                .HasColumnName("UCD_SCHOOL_DIVISION");
        });

        modelBuilder.Entity<JobActionDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JOB_ACTION_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.JobActnCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_ACTN_CD");
            entity.Property(e => e.JobActnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_ACTN_D_KEY");
            entity.Property(e => e.JobActnDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_ACTN_DESC");
            entity.Property(e => e.JobActnEffDt).HasColumnName("JOB_ACTN_EFF_DT");
            entity.Property(e => e.JobActnRsnCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_ACTN_RSN_CD");
            entity.Property(e => e.JobActnRsnDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_ACTN_RSN_DESC");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<JobCodeDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JOB_CODE_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.JobCd)
                .HasMaxLength(6)
                .HasColumnName("JOB_CD");
            entity.Property(e => e.JobCdAcdmcCompSubgrpCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_ACDMC_COMP_SUBGRP_CD");
            entity.Property(e => e.JobCdAcdmcCompSubgrpDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_ACDMC_COMP_SUBGRP_DESC");
            entity.Property(e => e.JobCdBargUnitCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_CD_BARG_UNIT_CD");
            entity.Property(e => e.JobCdBargUnitDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_BARG_UNIT_DESC");
            entity.Property(e => e.JobCdByAgrmtFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_BY_AGRMT_FLG");
            entity.Property(e => e.JobCdClassCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_CLASS_CD");
            entity.Property(e => e.JobCdClassDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_CLASS_DESC");
            entity.Property(e => e.JobCdCmpyCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_CMPY_CD");
            entity.Property(e => e.JobCdCmpyDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_CMPY_DESC");
            entity.Property(e => e.JobCdCompFreqCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_CD_COMP_FREQ_CD");
            entity.Property(e => e.JobCdCompFreqDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_COMP_FREQ_DESC");
            entity.Property(e => e.JobCdCurrencyCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_CURRENCY_CD");
            entity.Property(e => e.JobCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_CD_D_KEY");
            entity.Property(e => e.JobCdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_DESC");
            entity.Property(e => e.JobCdEduGovAcdmcRankCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_EDU_GOV_ACDMC_RANK_CD");
            entity.Property(e => e.JobCdEduGovAcdmcRankDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_EDU_GOV_ACDMC_RANK_DESC");
            entity.Property(e => e.JobCdEe01Cd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_EE01_CD");
            entity.Property(e => e.JobCdEe01Desc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_EE01_DESC");
            entity.Property(e => e.JobCdEeoJobGrpCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_CD_EEO_JOB_GRP_CD");
            entity.Property(e => e.JobCdEffDt).HasColumnName("JOB_CD_EFF_DT");
            entity.Property(e => e.JobCdEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_EFF_STAT_CD");
            entity.Property(e => e.JobCdElgblOncallFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_ELGBL_ONCALL_FLG");
            entity.Property(e => e.JobCdElgblShiftDiffFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_ELGBL_SHIFT_DIFF_FLG");
            entity.Property(e => e.JobCdElgblSumrSlryFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_ELGBL_SUMR_SLRY_FLG");
            entity.Property(e => e.JobCdEmpClassCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_EMP_CLASS_CD");
            entity.Property(e => e.JobCdEmpClassDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_EMP_CLASS_DESC");
            entity.Property(e => e.JobCdFlsaStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_FLSA_STAT_CD");
            entity.Property(e => e.JobCdFlsaStatDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_FLSA_STAT_DESC");
            entity.Property(e => e.JobCdGrdCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_GRD_CD");
            entity.Property(e => e.JobCdGrdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_GRD_DESC");
            entity.Property(e => e.JobCdJobFmlyCd)
                .HasMaxLength(6)
                .HasColumnName("JOB_CD_JOB_FMLY_CD");
            entity.Property(e => e.JobCdJobFmlyDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_JOB_FMLY_DESC");
            entity.Property(e => e.JobCdJobFuncCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_JOB_FUNC_CD");
            entity.Property(e => e.JobCdJobFuncDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_JOB_FUNC_DESC");
            entity.Property(e => e.JobCdLastUpdtDt).HasColumnName("JOB_CD_LAST_UPDT_DT");
            entity.Property(e => e.JobCdOcuptnlSubgrpCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_OCUPTNL_SUBGRP_CD");
            entity.Property(e => e.JobCdOcuptnlSubgrpDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_OCUPTNL_SUBGRP_DESC");
            entity.Property(e => e.JobCdOffscaleFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_OFFSCALE_FLG");
            entity.Property(e => e.JobCdPosnMgmtCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_POSN_MGMT_CD");
            entity.Property(e => e.JobCdRetrmntSafetyDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_RETRMNT_SAFETY_DESC");
            entity.Property(e => e.JobCdRetrmntSafetyFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_RETRMNT_SAFETY_FLG");
            entity.Property(e => e.JobCdRglrTempCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_RGLR_TEMP_CD");
            entity.Property(e => e.JobCdRglrTempDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_RGLR_TEMP_DESC");
            entity.Property(e => e.JobCdRgltryRegnCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_CD_RGLTRY_REGN_CD");
            entity.Property(e => e.JobCdRgltryRegnDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_RGLTRY_REGN_DESC");
            entity.Property(e => e.JobCdSetId)
                .HasMaxLength(5)
                .HasColumnName("JOB_CD_SET_ID");
            entity.Property(e => e.JobCdShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOB_CD_SHORT_DESC");
            entity.Property(e => e.JobCdSlryAdminPlanCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_CD_SLRY_ADMIN_PLAN_CD");
            entity.Property(e => e.JobCdSlryAdminPlanDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_SLRY_ADMIN_PLAN_DESC");
            entity.Property(e => e.JobCdSlryGrdMaxRtAnnl)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_CD_SLRY_GRD_MAX_RT_ANNL");
            entity.Property(e => e.JobCdSlryGrdMidRtAnnl)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_CD_SLRY_GRD_MID_RT_ANNL");
            entity.Property(e => e.JobCdSlryGrdMinRtAnnl)
                .HasMaxLength(20)
                .HasColumnName("JOB_CD_SLRY_GRD_MIN_RT_ANNL");
            entity.Property(e => e.JobCdSlrySetId)
                .HasMaxLength(5)
                .HasColumnName("JOB_CD_SLRY_SET_ID");
            entity.Property(e => e.JobCdStdHrsFreqCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_CD_STD_HRS_FREQ_CD");
            entity.Property(e => e.JobCdStdHrsFreqDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_STD_HRS_FREQ_DESC");
            entity.Property(e => e.JobCdStdHrsQty)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("JOB_CD_STD_HRS_QTY");
            entity.Property(e => e.JobCdStepCd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_CD_STEP_CD");
            entity.Property(e => e.JobCdStepDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_STEP_DESC");
            entity.Property(e => e.JobCdSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JOB_CD__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JobCdSvmSeqMrf).HasColumnName("JOB_CD__SVM_SEQ_MRF");
            entity.Property(e => e.JobCdSvmSeqNum).HasColumnName("JOB_CD__SVM_SEQ_NUM");
            entity.Property(e => e.JobCdSwLocalCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_SW_LOCAL_CD");
            entity.Property(e => e.JobCdSwLocalDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_SW_LOCAL_DESC");
            entity.Property(e => e.JobCdTrngProgCd)
                .HasMaxLength(6)
                .HasColumnName("JOB_CD_TRNG_PROG_CD");
            entity.Property(e => e.JobCdTrngProgDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_TRNG_PROG_DESC");
            entity.Property(e => e.JobCdUcFacltyFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_UC_FACLTY_FLG");
            entity.Property(e => e.JobCdUcrpExclsnElgbltyFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_UCRP_EXCLSN_ELGBLTY_FLG");
            entity.Property(e => e.JobCdUnionCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_UNION_CD");
            entity.Property(e => e.JobCdUnionDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_UNION_DESC");
            entity.Property(e => e.JobCdUsOcuptnlClsfctnCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_CD_US_OCUPTNL_CLSFCTN_CD");
            entity.Property(e => e.JobCdUsOcuptnlClsfctnDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_US_OCUPTNL_CLSFCTN_DESC");
            entity.Property(e => e.JobCdUsStdOcuptnlCd)
                .HasMaxLength(10)
                .HasColumnName("JOB_CD_US_STD_OCUPTNL_CD");
            entity.Property(e => e.JobCdUsStdOcuptnlDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_US_STD_OCUPTNL_DESC");
            entity.Property(e => e.JobCdWrkCompCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_CD_WRK_COMP_CD");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<JobDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JOB_D_V");

            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.JobDAbsSysCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_ABS_SYS_CD");
            entity.Property(e => e.JobDAbsSysDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_ABS_SYS_DESC");
            entity.Property(e => e.JobDAcdmcDurtnApntmtCd)
                .HasMaxLength(2)
                .HasColumnName("JOB_D_ACDMC_DURTN_APNTMT_CD");
            entity.Property(e => e.JobDAcdmcDurtnApntmtDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_ACDMC_DURTN_APNTMT_DESC");
            entity.Property(e => e.JobDAutoJobTermntnEndFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_AUTO_JOB_TERMNTN_END_FLG");
            entity.Property(e => e.JobDBargUnitCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_D_BARG_UNIT_CD");
            entity.Property(e => e.JobDBargUnitDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_BARG_UNIT_DESC");
            entity.Property(e => e.JobDBasActnCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_BAS_ACTN_CD");
            entity.Property(e => e.JobDBaseGrpDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_BASE_GRP_DESC");
            entity.Property(e => e.JobDBaseGrpId)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_BASE_GRP_ID");
            entity.Property(e => e.JobDBenSysCd)
                .HasMaxLength(2)
                .HasColumnName("JOB_D_BEN_SYS_CD");
            entity.Property(e => e.JobDBenSysDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_BEN_SYS_DESC");
            entity.Property(e => e.JobDCertfdFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_CERTFD_FLG");
            entity.Property(e => e.JobDClassCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_CLASS_CD");
            entity.Property(e => e.JobDClassDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_CLASS_DESC");
            entity.Property(e => e.JobDCmpyCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_CMPY_CD");
            entity.Property(e => e.JobDCmpyDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_CMPY_DESC");
            entity.Property(e => e.JobDCntrctNum)
                .HasMaxLength(25)
                .HasColumnName("JOB_D_CNTRCT_NUM");
            entity.Property(e => e.JobDCobraActnCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_COBRA_ACTN_CD");
            entity.Property(e => e.JobDCompFreqCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_D_COMP_FREQ_CD");
            entity.Property(e => e.JobDCompFreqDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_D_COMP_FREQ_DESC");
            entity.Property(e => e.JobDCurrencyRtTypeCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_D_CURRENCY_RT_TYPE_CD");
            entity.Property(e => e.JobDDrctlyTippedCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_DRCTLY_TIPPED_CD");
            entity.Property(e => e.JobDDrctlyTippedDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_DRCTLY_TIPPED_DESC");
            entity.Property(e => e.JobDEeoClassCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_EEO_CLASS_CD");
            entity.Property(e => e.JobDEeoClassDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_EEO_CLASS_DESC");
            entity.Property(e => e.JobDEeoRptngRegnEstbDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_EEO_RPTNG_REGN_ESTB_DESC");
            entity.Property(e => e.JobDEeoRptngRegnEstbId)
                .HasMaxLength(12)
                .HasColumnName("JOB_D_EEO_RPTNG_REGN_ESTB_ID");
            entity.Property(e => e.JobDElgbltyCnfgrtn1Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_1_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn2Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_2_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn3Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_3_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn4Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_4_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn5Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_5_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn6Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_6_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn7Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_7_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn8Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_8_IND");
            entity.Property(e => e.JobDElgbltyCnfgrtn9Ind)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_ELGBLTY_CNFGRTN_9_IND");
            entity.Property(e => e.JobDEmpClassCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_EMP_CLASS_CD");
            entity.Property(e => e.JobDEmpClassDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_EMP_CLASS_DESC");
            entity.Property(e => e.JobDEmpTypeCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_EMP_TYPE_CD");
            entity.Property(e => e.JobDEmpTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_EMP_TYPE_DESC");
            entity.Property(e => e.JobDEncmbrncOvrdFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_ENCMBRNC_OVRD_FLG");
            entity.Property(e => e.JobDErngsDstrbtnTypeCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_ERNGS_DSTRBTN_TYPE_CD");
            entity.Property(e => e.JobDErngsDstrbtnTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_ERNGS_DSTRBTN_TYPE_DESC");
            entity.Property(e => e.JobDFicaStatEeCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_FICA_STAT_EE_CD");
            entity.Property(e => e.JobDFicaStatEeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_FICA_STAT_EE_DESC");
            entity.Property(e => e.JobDFlsaStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_FLSA_STAT_CD");
            entity.Property(e => e.JobDFlsaStatDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_FLSA_STAT_DESC");
            entity.Property(e => e.JobDFullPartCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_FULL_PART_CD");
            entity.Property(e => e.JobDFullPartDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_FULL_PART_DESC");
            entity.Property(e => e.JobDGpDfltElgbltyGrpFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_GP_DFLT_ELGBLTY_GRP_FLG");
            entity.Property(e => e.JobDGpElgbltyGrpCd)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_GP_ELGBLTY_GRP_CD");
            entity.Property(e => e.JobDGpOvrdDfltAsofDtFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_GP_OVRD_DFLT_ASOF_DT_FLG");
            entity.Property(e => e.JobDGpOvrdDfltRtTypeFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_GP_OVRD_DFLT_RT_TYPE_FLG");
            entity.Property(e => e.JobDGpPayGrpCd)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_GP_PAY_GRP_CD");
            entity.Property(e => e.JobDGpUseRtAsofDtCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_GP_USE_RT_ASOF_DT_CD");
            entity.Property(e => e.JobDGpUseRtAsofDtDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_GP_USE_RT_ASOF_DT_DESC");
            entity.Property(e => e.JobDGrdCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_GRD_CD");
            entity.Property(e => e.JobDGrdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_GRD_DESC");
            entity.Property(e => e.JobDHldyScheduleCd)
                .HasMaxLength(6)
                .HasColumnName("JOB_D_HLDY_SCHEDULE_CD");
            entity.Property(e => e.JobDHldyScheduleDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_HLDY_SCHEDULE_DESC");
            entity.Property(e => e.JobDJobDataSrcCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_JOB_DATA_SRC_CD");
            entity.Property(e => e.JobDJobDataSrcDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_JOB_DATA_SRC_DESC");
            entity.Property(e => e.JobDJobDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_JOB_DESC");
            entity.Property(e => e.JobDJobInd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_JOB_IND");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobDLastDtWrkedOvrdFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_LAST_DT_WRKED_OVRD_FLG");
            entity.Property(e => e.JobDLocCd)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_LOC_CD");
            entity.Property(e => e.JobDLocDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_LOC_DESC");
            entity.Property(e => e.JobDLumpSumPayFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_LUMP_SUM_PAY_FLG");
            entity.Property(e => e.JobDOffcrCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_OFFCR_CD");
            entity.Property(e => e.JobDOffcrDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_OFFCR_DESC");
            entity.Property(e => e.JobDPersOrgCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_PERS_ORG_CD");
            entity.Property(e => e.JobDPersOrgCdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_PERS_ORG_CD_DESC");
            entity.Property(e => e.JobDPoiTypeCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_D_POI_TYPE_CD");
            entity.Property(e => e.JobDPoiTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_POI_TYPE_DESC");
            entity.Property(e => e.JobDPosnChngRecFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_POSN_CHNG_REC_FLG");
            entity.Property(e => e.JobDPosnOvrdFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_POSN_OVRD_FLG");
            entity.Property(e => e.JobDPrbtnCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_PRBTN_CD");
            entity.Property(e => e.JobDPrbtnDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_PRBTN_DESC");
            entity.Property(e => e.JobDProrateCountAmtCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_PRORATE_COUNT_AMT_CD");
            entity.Property(e => e.JobDProrateCountAmtDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_PRORATE_COUNT_AMT_DESC");
            entity.Property(e => e.JobDPyrlSysCd)
                .HasMaxLength(2)
                .HasColumnName("JOB_D_PYRL_SYS_CD");
            entity.Property(e => e.JobDPyrlSysDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_PYRL_SYS_DESC");
            entity.Property(e => e.JobDRegnCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_D_REGN_CD");
            entity.Property(e => e.JobDRegnDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_D_REGN_DESC");
            entity.Property(e => e.JobDRglrTempCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_RGLR_TEMP_CD");
            entity.Property(e => e.JobDRglrTempDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_RGLR_TEMP_DESC");
            entity.Property(e => e.JobDRptsToCd)
                .HasMaxLength(8)
                .HasColumnName("JOB_D_RPTS_TO_CD");
            entity.Property(e => e.JobDSalAdmnPlanCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_D_SAL_ADMN_PLAN_CD");
            entity.Property(e => e.JobDSalAdmnPlanDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_SAL_ADMN_PLAN_DESC");
            entity.Property(e => e.JobDShiftCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_SHIFT_CD");
            entity.Property(e => e.JobDShiftDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_SHIFT_DESC");
            entity.Property(e => e.JobDSlryGrdMaxRtAnnl)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_D_SLRY_GRD_MAX_RT_ANNL");
            entity.Property(e => e.JobDSlryGrdMidRtAnnl)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_D_SLRY_GRD_MID_RT_ANNL");
            entity.Property(e => e.JobDSlryGrdMinRtAnnl)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_D_SLRY_GRD_MIN_RT_ANNL");
            entity.Property(e => e.JobDStdHrsFreqCd)
                .HasMaxLength(5)
                .HasColumnName("JOB_D_STD_HRS_FREQ_CD");
            entity.Property(e => e.JobDStepNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_STEP_NUM");
            entity.Property(e => e.JobDSupvsrLvlDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_SUPVSR_LVL_DESC");
            entity.Property(e => e.JobDSupvsrLvlId)
                .HasMaxLength(8)
                .HasColumnName("JOB_D_SUPVSR_LVL_ID");
            entity.Property(e => e.JobDTxLocCd)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_TX_LOC_CD");
            entity.Property(e => e.JobDTxLocDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_TX_LOC_DESC");
            entity.Property(e => e.JobDUcAltWrkWkCd)
                .HasMaxLength(4)
                .HasColumnName("JOB_D_UC_ALT_WRK_WK_CD");
            entity.Property(e => e.JobDUcAltWrkWkDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_UC_ALT_WRK_WK_DESC");
            entity.Property(e => e.JobDUcElgblGrpOvrdCd)
                .HasMaxLength(10)
                .HasColumnName("JOB_D_UC_ELGBL_GRP_OVRD_CD");
            entity.Property(e => e.JobDUcLocUseTypeCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_UC_LOC_USE_TYPE_CD");
            entity.Property(e => e.JobDUcLocUseTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_UC_LOC_USE_TYPE_DESC");
            entity.Property(e => e.JobDUcPayGrpOvrdCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_UC_PAY_GRP_OVRD_CD");
            entity.Property(e => e.JobDUcPayGrpOvrdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_UC_PAY_GRP_OVRD_DESC");
            entity.Property(e => e.JobDUcPrmyJobOvrdFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_UC_PRMY_JOB_OVRD_FLG");
            entity.Property(e => e.JobDUcPyCarDurCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_D_UC_PY_CAR_DUR_CD");
            entity.Property(e => e.JobDUcPyCarDurDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_D_UC_PY_CAR_DUR_DESC");
            entity.Property(e => e.JobDUcTermOvrdFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_UC_TERM_OVRD_FLG");
            entity.Property(e => e.JobDUnionLocalFlg)
                .HasMaxLength(1)
                .HasColumnName("JOB_D_UNION_LOCAL_FLG");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<JobFV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JOB_F_V");

            entity.Property(e => e.AsgnmtEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ASGNMT_END_DT_KEY");
            entity.Property(e => e.AsgnmtStartDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ASGNMT_START_DT_KEY");
            entity.Property(e => e.BusUnitDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BUS_UNIT_D_KEY");
            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(7)
                .HasColumnName("DEPT_CODE");
            entity.Property(e => e.DeptDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_D_KEY");
            entity.Property(e => e.DeptEntryDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_ENTRY_DT_KEY");
            entity.Property(e => e.DeptShortTitle)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHORT_TITLE");
            entity.Property(e => e.DeptTitle)
                .HasMaxLength(40)
                .HasColumnName("DEPT_TITLE");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EffDt).HasColumnName("EFF_DT");
            entity.Property(e => e.EffDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFF_DT_KEY");
            entity.Property(e => e.EmpDCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_CUR_KEY");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpDSupvsrKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_SUPVSR_KEY");
            entity.Property(e => e.ExpctdEndDate).HasColumnName("EXPCTD_END_DATE");
            entity.Property(e => e.ExpctdEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EXPCTD_END_DT_KEY");
            entity.Property(e => e.ExpctdRtrnDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EXPCTD_RTRN_DT_KEY");
            entity.Property(e => e.GrdEntryDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("GRD_ENTRY_DT_KEY");
            entity.Property(e => e.HireDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("HIRE_DT_KEY");
            entity.Property(e => e.JobActnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_ACTN_D_KEY");
            entity.Property(e => e.JobActnDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_ACTN_DT_KEY");
            entity.Property(e => e.JobActnEffDt).HasColumnName("JOB_ACTN_EFF_DT");
            entity.Property(e => e.JobCdDCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_CD_D_CUR_KEY");
            entity.Property(e => e.JobCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_CD_D_KEY");
            entity.Property(e => e.JobCode)
                .HasMaxLength(6)
                .HasColumnName("JOB_CODE");
            entity.Property(e => e.JobCodeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CODE_DESC");
            entity.Property(e => e.JobCodeFlsaStat)
                .HasMaxLength(1)
                .HasColumnName("JOB_CODE_FLSA_STAT");
            entity.Property(e => e.JobCodeShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOB_CODE_SHORT_DESC");
            entity.Property(e => e.JobCodeUnionDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CODE_UNION_DESC");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobEntryDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_ENTRY_DT_KEY");
            entity.Property(e => e.JobFAnnlBenBaseRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_ANNL_BEN_BASE_RT");
            entity.Property(e => e.JobFAnnlRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_ANNL_RT");
            entity.Property(e => e.JobFChngAmt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("JOB_F_CHNG_AMT");
            entity.Property(e => e.JobFChngPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("JOB_F_CHNG_PCT");
            entity.Property(e => e.JobFCompRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("JOB_F_COMP_RT");
            entity.Property(e => e.JobFDailyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_DAILY_RT");
            entity.Property(e => e.JobFEffSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_EFF_SEQ_NUM");
            entity.Property(e => e.JobFEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_EMP_REC_NUM");
            entity.Property(e => e.JobFFtePct)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("JOB_F_FTE_PCT");
            entity.Property(e => e.JobFHrlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("JOB_F_HRLY_RT");
            entity.Property(e => e.JobFJobCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_JOB_CNT");
            entity.Property(e => e.JobFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_KEY");
            entity.Property(e => e.JobFMthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_MTHLY_RT");
            entity.Property(e => e.JobFShiftFactorPct)
                .HasColumnType("numeric(4, 3)")
                .HasColumnName("JOB_F_SHIFT_FACTOR_PCT");
            entity.Property(e => e.JobFShiftRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("JOB_F_SHIFT_RT");
            entity.Property(e => e.JobFStdHrsQty)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("JOB_F_STD_HRS_QTY");
            entity.Property(e => e.JobFSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JOB_F__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JobFSvmPrimaryIdx)
                .HasMaxLength(2)
                .HasColumnName("JOB_F__SVM_PRIMARY_IDX");
            entity.Property(e => e.JobFSvmSeqMrf).HasColumnName("JOB_F__SVM_SEQ_MRF");
            entity.Property(e => e.JobFSvmSeqNum).HasColumnName("JOB_F__SVM_SEQ_NUM");
            entity.Property(e => e.JobFWrkDayHrsQty)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("JOB_F_WRK_DAY_HRS_QTY");
            entity.Property(e => e.JobGroup)
                .HasMaxLength(3)
                .HasColumnName("JOB_GROUP");
            entity.Property(e => e.JobGroupDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_GROUP_DESC");
            entity.Property(e => e.JobStatDBenKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_BEN_KEY");
            entity.Property(e => e.JobStatDEmpKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_EMP_KEY");
            entity.Property(e => e.JobStatDHrKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_HR_KEY");
            entity.Property(e => e.JobStatusCode)
                .HasMaxLength(1)
                .HasColumnName("JOB_STATUS_CODE");
            entity.Property(e => e.JobStatusDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STATUS_DESC");
            entity.Property(e => e.LastAsgnmtStartDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("LAST_ASGNMT_START_DT_KEY");
            entity.Property(e => e.LastDayWrkedDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("LAST_DAY_WRKED_DT_KEY");
            entity.Property(e => e.LastHireDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("LAST_HIRE_DT_KEY");
            entity.Property(e => e.OrgDHmCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_HM_CUR_KEY");
            entity.Property(e => e.OrgDHmKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_HM_KEY");
            entity.Property(e => e.OrgDJobCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_JOB_CUR_KEY");
            entity.Property(e => e.OrgDJobKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_JOB_KEY");
            entity.Property(e => e.PayGrpDCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PAY_GRP_D_CUR_KEY");
            entity.Property(e => e.PayGrpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PAY_GRP_D_KEY");
            entity.Property(e => e.PosnDCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_D_CUR_KEY");
            entity.Property(e => e.PosnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_D_KEY");
            entity.Property(e => e.PosnEntryDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_ENTRY_DT_KEY");
            entity.Property(e => e.PosnNum)
                .HasMaxLength(8)
                .HasColumnName("POSN_NUM");
            entity.Property(e => e.PrbtnEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRBTN_END_DT_KEY");
            entity.Property(e => e.PrmyJobsDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRMY_JOBS_D_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
            entity.Property(e => e.StepEntryDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("STEP_ENTRY_DT_KEY");
            entity.Property(e => e.SubDivCode)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CODE");
            entity.Property(e => e.SubDivTitle)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_TITLE");
            entity.Property(e => e.TermntnDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("TERMNTN_DT_KEY");
            entity.Property(e => e.TrialEmplymtEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("TRIAL_EMPLYMT_END_DT_KEY");
            entity.Property(e => e.UcEmpRedTimeEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UC_EMP_RED_TIME_END_DT_KEY");
            entity.Property(e => e.UcEmpRevwDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UC_EMP_REVW_D_KEY");
            entity.Property(e => e.UcLocUseEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UC_LOC_USE_END_DT_KEY");
            entity.Property(e => e.UcPostDoctAnvsDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UC_POST_DOCT_ANVS_DT_KEY");
            entity.Property(e => e.UnionDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UNION_D_KEY");
        });

        modelBuilder.Entity<JobHistory>(entity =>
        {
            entity.HasKey(e => e.Recordid);

            entity.ToTable("jobHistory");

            entity.Property(e => e.Recordid).HasColumnName("recordid");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("emp_id");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("emp_pps_uid");
            entity.Property(e => e.FirstDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("first_dept");
            entity.Property(e => e.FirstEffDt)
                .HasColumnType("datetime")
                .HasColumnName("first_eff_dt");
            entity.Property(e => e.FirstEndDate)
                .HasColumnType("datetime")
                .HasColumnName("first_end_date");
            entity.Property(e => e.FirstSeen)
                .HasColumnType("datetime")
                .HasColumnName("first_seen");
            entity.Property(e => e.FirstTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_title");
            entity.Property(e => e.JobCd)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("job_cd");
            entity.Property(e => e.JobGrp)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("job_grp");
            entity.Property(e => e.LastDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("last_dept");
            entity.Property(e => e.LastEffDt)
                .HasColumnType("datetime")
                .HasColumnName("last_eff_dt");
            entity.Property(e => e.LastEndDate)
                .HasColumnType("datetime")
                .HasColumnName("last_end_date");
            entity.Property(e => e.LastSeen)
                .HasColumnType("datetime")
                .HasColumnName("last_seen");
            entity.Property(e => e.LastStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("last_status");
            entity.Property(e => e.LastTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("last_title");
            entity.Property(e => e.RecNum).HasColumnName("rec_num");
        });

        modelBuilder.Entity<JobHistoryDetail>(entity =>
        {
            entity.HasKey(e => e.Recordid);

            entity.ToTable("jobHistoryDetail");

            entity.Property(e => e.Recordid).HasColumnName("recordid");
            entity.Property(e => e.ActionCd)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("action_cd");
            entity.Property(e => e.ActionEffDt)
                .HasColumnType("datetime")
                .HasColumnName("action_eff_dt");
            entity.Property(e => e.ActionRsnCd)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("action_rsn_cd");
            entity.Property(e => e.AnnlRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("annl_rt");
            entity.Property(e => e.CurFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("cur_flag");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("dept_cd");
            entity.Property(e => e.EffDt)
                .HasColumnType("datetime")
                .HasColumnName("eff_dt");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("emp_id");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("emp_pps_uid");
            entity.Property(e => e.EndDt)
                .HasColumnType("datetime")
                .HasColumnName("end_dt");
            entity.Property(e => e.FirstSeen)
                .HasColumnType("datetime")
                .HasColumnName("first_seen");
            entity.Property(e => e.FtePct)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("fte_pct");
            entity.Property(e => e.InsertDttm)
                .HasColumnType("datetime")
                .HasColumnName("insert_dttm");
            entity.Property(e => e.JobCd)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("job_cd");
            entity.Property(e => e.LastSeen)
                .HasColumnType("datetime")
                .HasColumnName("last_seen");
            entity.Property(e => e.RecNum).HasColumnName("rec_num");
            entity.Property(e => e.SeqNum).HasColumnName("seq_num");
            entity.Property(e => e.SrcDttm)
                .HasColumnType("datetime")
                .HasColumnName("src_dttm");
            entity.Property(e => e.StatCd)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("stat_cd");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.SvmCurFlag).HasColumnName("svm_cur_flag");
            entity.Property(e => e.SvmSeqNum).HasColumnName("svm_seq_num");
            entity.Property(e => e.UpdateDttm)
                .HasColumnType("datetime")
                .HasColumnName("update_dttm");
        });

        modelBuilder.Entity<JobOverride>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK_jobStatusOverride");

            entity.ToTable("jobOverride");

            entity.Property(e => e.RecordId).HasColumnName("recordID");
            entity.Property(e => e.AddedBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("addedBy");
            entity.Property(e => e.AddedOn)
                .HasColumnType("datetime")
                .HasColumnName("addedOn");
            entity.Property(e => e.Comment)
                .IsUnicode(false)
                .HasColumnName("comment");
            entity.Property(e => e.Deptid)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("deptid");
            entity.Property(e => e.Effdt)
                .HasColumnType("datetime")
                .HasColumnName("effdt");
            entity.Property(e => e.Effseq).HasColumnName("effseq");
            entity.Property(e => e.EmplRcd).HasColumnName("empl_rcd");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("emplid");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("jobcode");
            entity.Property(e => e.Jobstatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("jobstatus");
            entity.Property(e => e.LastmodifiedBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("lastmodifiedBy");
            entity.Property(e => e.LastmodifiedOn)
                .HasColumnType("datetime")
                .HasColumnName("lastmodifiedOn");
        });

        modelBuilder.Entity<JobStatusDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JOB_STATUS_D_V");

            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.JobStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_STAT_CD");
            entity.Property(e => e.JobStatDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_KEY");
            entity.Property(e => e.JobStatDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STAT_DESC");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<JpmFV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JPM_F_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpDCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_CUR_KEY");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.JpmCtlgDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_CTLG_D_KEY");
            entity.Property(e => e.JpmFCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_F_CNT");
            entity.Property(e => e.JpmFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_F_KEY");
            entity.Property(e => e.JpmFSvmRecIdx).HasColumnName("JPM_F__SVM_REC_IDX");
            entity.Property(e => e.JpmItemDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_ITEM_D_KEY");
            entity.Property(e => e.JpmProfileDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_PROFILE_D_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<JpmJpItemDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("JPM_JP_ITEM_D_V");

            entity.Property(e => e.DdwMd5Type1)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE1");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.JpmItemDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_ITEM_D_KEY");
            entity.Property(e => e.JpmJpItem1Dcml)
                .HasColumnType("numeric(9, 2)")
                .HasColumnName("JPM_JP_ITEM_1_DCML");
            entity.Property(e => e.JpmJpItem1Dt).HasColumnName("JPM_JP_ITEM_1_DT");
            entity.Property(e => e.JpmJpItem1Flg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_1_FLG");
            entity.Property(e => e.JpmJpItem1Intgr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_JP_ITEM_1_INTGR");
            entity.Property(e => e.JpmJpItem1LargeTxt)
                .HasMaxLength(1325)
                .HasColumnName("JPM_JP_ITEM_1_LARGE_TXT");
            entity.Property(e => e.JpmJpItem1Pct)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_JP_ITEM_1_PCT");
            entity.Property(e => e.JpmJpItem1Txt)
                .HasMaxLength(254)
                .HasColumnName("JPM_JP_ITEM_1_TXT");
            entity.Property(e => e.JpmJpItem2Dcml)
                .HasColumnType("numeric(9, 2)")
                .HasColumnName("JPM_JP_ITEM_2_DCML");
            entity.Property(e => e.JpmJpItem2Dt).HasColumnName("JPM_JP_ITEM_2_DT");
            entity.Property(e => e.JpmJpItem2Flg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_2_FLG");
            entity.Property(e => e.JpmJpItem2Intgr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_JP_ITEM_2_INTGR");
            entity.Property(e => e.JpmJpItem2LargeTxt)
                .HasMaxLength(1325)
                .HasColumnName("JPM_JP_ITEM_2_LARGE_TXT");
            entity.Property(e => e.JpmJpItem2Pct)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_JP_ITEM_2_PCT");
            entity.Property(e => e.JpmJpItem2QualId)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_2_QUAL_ID");
            entity.Property(e => e.JpmJpItem2Txt)
                .HasMaxLength(254)
                .HasColumnName("JPM_JP_ITEM_2_TXT");
            entity.Property(e => e.JpmJpItem3Dt).HasColumnName("JPM_JP_ITEM_3_DT");
            entity.Property(e => e.JpmJpItem3Flg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_3_FLG");
            entity.Property(e => e.JpmJpItem3Txt)
                .HasMaxLength(254)
                .HasColumnName("JPM_JP_ITEM_3_TXT");
            entity.Property(e => e.JpmJpItem4Dt).HasColumnName("JPM_JP_ITEM_4_DT");
            entity.Property(e => e.JpmJpItem4Flg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_4_FLG");
            entity.Property(e => e.JpmJpItem4Txt)
                .HasMaxLength(254)
                .HasColumnName("JPM_JP_ITEM_4_TXT");
            entity.Property(e => e.JpmJpItem5Dt).HasColumnName("JPM_JP_ITEM_5_DT");
            entity.Property(e => e.JpmJpItem5Flg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_5_FLG");
            entity.Property(e => e.JpmJpItem5Txt)
                .HasMaxLength(254)
                .HasColumnName("JPM_JP_ITEM_5_TXT");
            entity.Property(e => e.JpmJpItem6Dt).HasColumnName("JPM_JP_ITEM_6_DT");
            entity.Property(e => e.JpmJpItemAdhocDesc)
                .HasMaxLength(80)
                .HasColumnName("JPM_JP_ITEM_ADHOC_DESC");
            entity.Property(e => e.JpmJpItemAreaPref1)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM_AREA_PREF_1");
            entity.Property(e => e.JpmJpItemAreaPref2)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM_AREA_PREF_2");
            entity.Property(e => e.JpmJpItemAreaPref3)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM_AREA_PREF_3");
            entity.Property(e => e.JpmJpItemAvgGrdInd)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_AVG_GRD_IND");
            entity.Property(e => e.JpmJpItemBusUnitCd)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_BUS_UNIT_CD");
            entity.Property(e => e.JpmJpItemCntryCd)
                .HasMaxLength(3)
                .HasColumnName("JPM_JP_ITEM_CNTRY_CD");
            entity.Property(e => e.JpmJpItemCntryPref1)
                .HasMaxLength(3)
                .HasColumnName("JPM_JP_ITEM_CNTRY_PREF_1");
            entity.Property(e => e.JpmJpItemCntryPref2)
                .HasMaxLength(3)
                .HasColumnName("JPM_JP_ITEM_CNTRY_PREF_2");
            entity.Property(e => e.JpmJpItemCntryPref3)
                .HasMaxLength(3)
                .HasColumnName("JPM_JP_ITEM_CNTRY_PREF_3");
            entity.Property(e => e.JpmJpItemCtgyItemCd)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_CTGY_ITEM_CD");
            entity.Property(e => e.JpmJpItemCtgyItemDesc)
                .HasMaxLength(30)
                .HasColumnName("JPM_JP_ITEM_CTGY_ITEM_DESC");
            entity.Property(e => e.JpmJpItemCtgyTypeCd)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_CTGY_TYPE_CD");
            entity.Property(e => e.JpmJpItemCtgyTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("JPM_JP_ITEM_CTGY_TYPE_DESC");
            entity.Property(e => e.JpmJpItemDegreeReqrFlg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_DEGREE_REQR_FLG");
            entity.Property(e => e.JpmJpItemDeptId)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_DEPT_ID");
            entity.Property(e => e.JpmJpItemDeptSetId)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_DEPT_SET_ID");
            entity.Property(e => e.JpmJpItemEffDt).HasColumnName("JPM_JP_ITEM_EFF_DT");
            entity.Property(e => e.JpmJpItemEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_EFF_STAT_CD");
            entity.Property(e => e.JpmJpItemElvtnId)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM_ELVTN_ID");
            entity.Property(e => e.JpmJpItemElvtnMthdCd)
                .HasMaxLength(4)
                .HasColumnName("JPM_JP_ITEM_ELVTN_MTHD_CD");
            entity.Property(e => e.JpmJpItemEpAppraisalId)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_JP_ITEM_EP_APPRAISAL_ID");
            entity.Property(e => e.JpmJpItemFpSubjInd)
                .HasMaxLength(3)
                .HasColumnName("JPM_JP_ITEM_FP_SUBJ_IND");
            entity.Property(e => e.JpmJpItemImprtncInd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_IMPRTNC_IND");
            entity.Property(e => e.JpmJpItemIntrstLvlInd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_INTRST_LVL_IND");
            entity.Property(e => e.JpmJpItemIpeSwInd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_IPE_SW_IND");
            entity.Property(e => e.JpmJpItemJpmProfileId)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_JPM_PROFILE_ID");
            entity.Property(e => e.JpmJpItemKeyId)
                .HasColumnType("numeric(12, 0)")
                .HasColumnName("JPM_JP_ITEM_KEY_ID");
            entity.Property(e => e.JpmJpItemLoc1)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_LOC_1");
            entity.Property(e => e.JpmJpItemLoc2)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_LOC_2");
            entity.Property(e => e.JpmJpItemLocBunit1)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_LOC_BUNIT_1");
            entity.Property(e => e.JpmJpItemLocBunit2)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_LOC_BUNIT_2");
            entity.Property(e => e.JpmJpItemLocCd)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_LOC_CD");
            entity.Property(e => e.JpmJpItemLocSetId)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_LOC_SET_ID");
            entity.Property(e => e.JpmJpItemMajorCd)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_MAJOR_CD");
            entity.Property(e => e.JpmJpItemMajorDesc)
                .HasMaxLength(100)
                .HasColumnName("JPM_JP_ITEM_MAJOR_DESC");
            entity.Property(e => e.JpmJpItemMandatoryInd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_MANDATORY_IND");
            entity.Property(e => e.JpmJpItemMinorCd)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_MINOR_CD");
            entity.Property(e => e.JpmJpItemMinorDesc)
                .HasMaxLength(100)
                .HasColumnName("JPM_JP_ITEM_MINOR_DESC");
            entity.Property(e => e.JpmJpItemNvqStatCd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_NVQ_STAT_CD");
            entity.Property(e => e.JpmJpItemObstacle1Ind)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM_OBSTACLE_1_IND");
            entity.Property(e => e.JpmJpItemParentKeyId)
                .HasColumnType("numeric(12, 0)")
                .HasColumnName("JPM_JP_ITEM_PARENT_KEY_ID");
            entity.Property(e => e.JpmJpItemPers1Id)
                .HasMaxLength(11)
                .HasColumnName("JPM_JP_ITEM_PERS_1_ID");
            entity.Property(e => e.JpmJpItemPrompt10Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_10_IND");
            entity.Property(e => e.JpmJpItemPrompt11Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_11_IND");
            entity.Property(e => e.JpmJpItemPrompt12Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_12_IND");
            entity.Property(e => e.JpmJpItemPrompt13Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_13_IND");
            entity.Property(e => e.JpmJpItemPrompt14Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_14_IND");
            entity.Property(e => e.JpmJpItemPrompt15Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_15_IND");
            entity.Property(e => e.JpmJpItemPrompt16Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_16_IND");
            entity.Property(e => e.JpmJpItemPrompt17Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_17_IND");
            entity.Property(e => e.JpmJpItemPrompt18Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_18_IND");
            entity.Property(e => e.JpmJpItemPrompt19Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_19_IND");
            entity.Property(e => e.JpmJpItemPrompt1Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_1_IND");
            entity.Property(e => e.JpmJpItemPrompt20Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_20_IND");
            entity.Property(e => e.JpmJpItemPrompt2Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_2_IND");
            entity.Property(e => e.JpmJpItemPrompt3Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_3_IND");
            entity.Property(e => e.JpmJpItemPrompt4Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_4_IND");
            entity.Property(e => e.JpmJpItemPrompt5Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_5_IND");
            entity.Property(e => e.JpmJpItemPrompt6Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_6_IND");
            entity.Property(e => e.JpmJpItemPrompt7Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_7_IND");
            entity.Property(e => e.JpmJpItemPrompt8Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_8_IND");
            entity.Property(e => e.JpmJpItemPrompt9Ind)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_PROMPT_9_IND");
            entity.Property(e => e.JpmJpItemQualId)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_QUAL_ID");
            entity.Property(e => e.JpmJpItemQualSet2Id)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_QUAL_SET_2_ID");
            entity.Property(e => e.JpmJpItemQualSetId)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_QUAL_SET_ID");
            entity.Property(e => e.JpmJpItemRating1Ind)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_RATING_1_IND");
            entity.Property(e => e.JpmJpItemRating2Ind)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_RATING_2_IND");
            entity.Property(e => e.JpmJpItemRating3Ind)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_RATING_3_IND");
            entity.Property(e => e.JpmJpItemRatingModelCd)
                .HasMaxLength(4)
                .HasColumnName("JPM_JP_ITEM_RATING_MODEL_CD");
            entity.Property(e => e.JpmJpItemSchlCd)
                .HasMaxLength(10)
                .HasColumnName("JPM_JP_ITEM_SCHL_CD");
            entity.Property(e => e.JpmJpItemSchlDesc)
                .HasMaxLength(100)
                .HasColumnName("JPM_JP_ITEM_SCHL_DESC");
            entity.Property(e => e.JpmJpItemSchlTypeCd)
                .HasMaxLength(3)
                .HasColumnName("JPM_JP_ITEM_SCHL_TYPE_CD");
            entity.Property(e => e.JpmJpItemSetidLoc1)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_SETID_LOC_1");
            entity.Property(e => e.JpmJpItemSetidLoc2)
                .HasMaxLength(5)
                .HasColumnName("JPM_JP_ITEM_SETID_LOC_2");
            entity.Property(e => e.JpmJpItemSkillHiringFlg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_SKILL_HIRING_FLG");
            entity.Property(e => e.JpmJpItemSkillPrmtnFlg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_SKILL_PRMTN_FLG");
            entity.Property(e => e.JpmJpItemSkillTenureFlg)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_SKILL_TENURE_FLG");
            entity.Property(e => e.JpmJpItemSrc1Id)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_SRC_1_ID");
            entity.Property(e => e.JpmJpItemSrc2Id)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_SRC_2_ID");
            entity.Property(e => e.JpmJpItemSrc3Id)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_ITEM_SRC_3_ID");
            entity.Property(e => e.JpmJpItemSrcCd)
                .HasMaxLength(4)
                .HasColumnName("JPM_JP_ITEM_SRC_CD");
            entity.Property(e => e.JpmJpItemStCd)
                .HasMaxLength(6)
                .HasColumnName("JPM_JP_ITEM_ST_CD");
            entity.Property(e => e.JpmJpItemSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JpmJpItemSvmSeqMrf).HasColumnName("JPM_JP_ITEM__SVM_SEQ_MRF");
            entity.Property(e => e.JpmJpItemSvmSeqNum).HasColumnName("JPM_JP_ITEM__SVM_SEQ_NUM");
            entity.Property(e => e.JpmJpItemVrfyMthdInd)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_ITEM_VRFY_MTHD_IND");
            entity.Property(e => e.JpmJpItemWrkflwStatCd)
                .HasMaxLength(2)
                .HasColumnName("JPM_JP_ITEM_WRKFLW_STAT_CD");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<OdsEmployeeFlagsDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ODS_EMPLOYEE_FLAGS_D_V");

            entity.HasIndex(e => e.EmpId, "ucpathods_ods_employee_flags_emplid");

            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpAcdmcStdtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_STDT_FLG");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMgrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MGR_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspFloaterFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLOATER_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpSupvrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SUPVR_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(11)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.FlagsSvmPrimary)
                .HasMaxLength(2)
                .HasColumnName("FLAGS__SVM_PRIMARY");
            entity.Property(e => e.FlagsSvmSeqNum).HasColumnName("FLAGS__SVM_SEQ_NUM");
            entity.Property(e => e.LoadDt).HasColumnName("LOAD_DT");
            entity.Property(e => e.LoadId)
                .HasMaxLength(32)
                .HasColumnName("LOAD_ID");
        });

        modelBuilder.Entity<OrganizationDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ORGANIZATION_D_V");

            entity.Property(e => e.DdwLastUpdDt).HasColumnName("DDW_LAST_UPD_DT");
            entity.Property(e => e.DeptActDt).HasColumnName("DEPT_ACT_DT");
            entity.Property(e => e.DeptAddr1)
                .HasMaxLength(30)
                .HasColumnName("DEPT_ADDR_1");
            entity.Property(e => e.DeptAddr2)
                .HasMaxLength(20)
                .HasColumnName("DEPT_ADDR_2");
            entity.Property(e => e.DeptAddr3)
                .HasMaxLength(20)
                .HasColumnName("DEPT_ADDR_3");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptInactDt).HasColumnName("DEPT_INACT_DT");
            entity.Property(e => e.DeptMailCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_MAIL_CD");
            entity.Property(e => e.DeptShrtTtl)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHRT_TTL");
            entity.Property(e => e.DeptTtl)
                .HasMaxLength(40)
                .HasColumnName("DEPT_TTL");
            entity.Property(e => e.DivCd)
                .HasMaxLength(6)
                .HasColumnName("DIV_CD");
            entity.Property(e => e.DivTtl)
                .HasMaxLength(40)
                .HasColumnName("DIV_TTL");
            entity.Property(e => e.LocCd)
                .HasMaxLength(1)
                .HasColumnName("LOC_CD");
            entity.Property(e => e.LocDesc)
                .HasMaxLength(30)
                .HasColumnName("LOC_DESC");
            entity.Property(e => e.OrgCd)
                .HasMaxLength(5)
                .HasColumnName("ORG_CD");
            entity.Property(e => e.OrgDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_KEY");
            entity.Property(e => e.OrgEffDt).HasColumnName("ORG_EFF_DT");
            entity.Property(e => e.OrgExprDt).HasColumnName("ORG_EXPR_DT");
            entity.Property(e => e.OrgSrcDesc)
                .HasMaxLength(20)
                .HasColumnName("ORG_SRC_DESC");
            entity.Property(e => e.OrgTtl)
                .HasMaxLength(40)
                .HasColumnName("ORG_TTL");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivTtl)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_TTL");
        });

        modelBuilder.Entity<PayGroupDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PAY_GROUP_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.PayGrp1042ErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_1042_ERNGS_CD");
            entity.Property(e => e.PayGrpAutoPaysheetUpdtFlg)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_AUTO_PAYSHEET_UPDT_FLG");
            entity.Property(e => e.PayGrpCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_CD");
            entity.Property(e => e.PayGrpCmpyCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_CMPY_CD");
            entity.Property(e => e.PayGrpCnfrmErrCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_CNFRM_ERR_CD");
            entity.Property(e => e.PayGrpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PAY_GRP_D_KEY");
            entity.Property(e => e.PayGrpDdSrcBankId)
                .HasMaxLength(8)
                .HasColumnName("PAY_GRP_DD_SRC_BANK_ID");
            entity.Property(e => e.PayGrpDedctnPrtyNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PAY_GRP_DEDCTN_PRTY_NUM");
            entity.Property(e => e.PayGrpDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_DESC");
            entity.Property(e => e.PayGrpDfltBnpgmCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_DFLT_BNPGM_CD");
            entity.Property(e => e.PayGrpDfltSetidCd)
                .HasMaxLength(5)
                .HasColumnName("PAY_GRP_DFLT_SETID_CD");
            entity.Property(e => e.PayGrpDfltSetidDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_DFLT_SETID_DESC");
            entity.Property(e => e.PayGrpEffDt).HasColumnName("PAY_GRP_EFF_DT");
            entity.Property(e => e.PayGrpEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_EFF_STAT_CD");
            entity.Property(e => e.PayGrpEmpTypeCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_EMP_TYPE_CD");
            entity.Property(e => e.PayGrpEmpTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_EMP_TYPE_DESC");
            entity.Property(e => e.PayGrpErrPedOptnCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_ERR_PED_OPTN_CD");
            entity.Property(e => e.PayGrpErrPedOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_ERR_PED_OPTN_DESC");
            entity.Property(e => e.PayGrpFlsaBasicFrmlaCd)
                .HasMaxLength(2)
                .HasColumnName("PAY_GRP_FLSA_BASIC_FRMLA_CD");
            entity.Property(e => e.PayGrpFlsaBasicFrmlaDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_FLSA_BASIC_FRMLA_DESC");
            entity.Property(e => e.PayGrpFlsaCalId)
                .HasMaxLength(10)
                .HasColumnName("PAY_GRP_FLSA_CAL_ID");
            entity.Property(e => e.PayGrpFlsaPerTypeCd)
                .HasMaxLength(2)
                .HasColumnName("PAY_GRP_FLSA_PER_TYPE_CD");
            entity.Property(e => e.PayGrpFlsaReqrdDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_FLSA_REQRD_DESC");
            entity.Property(e => e.PayGrpFlsaReqrdFlg)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_FLSA_REQRD_FLG");
            entity.Property(e => e.PayGrpFlsaSlryHrUsedCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_FLSA_SLRY_HR_USED_CD");
            entity.Property(e => e.PayGrpFlsaSlryHrUsedDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_FLSA_SLRY_HR_USED_DESC");
            entity.Property(e => e.PayGrpFreqDailyId)
                .HasMaxLength(5)
                .HasColumnName("PAY_GRP_FREQ_DAILY_ID");
            entity.Property(e => e.PayGrpFreqMthlyId)
                .HasMaxLength(5)
                .HasColumnName("PAY_GRP_FREQ_MTHLY_ID");
            entity.Property(e => e.PayGrpHldyErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_HLDY_ERNGS_CD");
            entity.Property(e => e.PayGrpHldyScheduleCd)
                .HasMaxLength(6)
                .HasColumnName("PAY_GRP_HLDY_SCHEDULE_CD");
            entity.Property(e => e.PayGrpIndstryCd)
                .HasMaxLength(4)
                .HasColumnName("PAY_GRP_INDSTRY_CD");
            entity.Property(e => e.PayGrpIndstryDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_INDSTRY_DESC");
            entity.Property(e => e.PayGrpIndstrySectorCd)
                .HasMaxLength(4)
                .HasColumnName("PAY_GRP_INDSTRY_SECTOR_CD");
            entity.Property(e => e.PayGrpIndstrySectorDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_INDSTRY_SECTOR_DESC");
            entity.Property(e => e.PayGrpNetPayMaxAmt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PAY_GRP_NET_PAY_MAX_AMT");
            entity.Property(e => e.PayGrpNetPayMinAmt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PAY_GRP_NET_PAY_MIN_AMT");
            entity.Property(e => e.PayGrpOasdiErngsBreakAmt)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("PAY_GRP_OASDI_ERNGS_BREAK_AMT");
            entity.Property(e => e.PayGrpOasdiHighExmptRt)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("PAY_GRP_OASDI_HIGH_EXMPT_RT");
            entity.Property(e => e.PayGrpOasdiLowFactorRt)
                .HasColumnType("numeric(4, 4)")
                .HasColumnName("PAY_GRP_OASDI_LOW_FACTOR_RT");
            entity.Property(e => e.PayGrpOtHrsErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_OT_HRS_ERNGS_CD");
            entity.Property(e => e.PayGrpPayFreqCd)
                .HasMaxLength(5)
                .HasColumnName("PAY_GRP_PAY_FREQ_CD");
            entity.Property(e => e.PayGrpPayFreqDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PAY_FREQ_DESC");
            entity.Property(e => e.PayGrpPayRptEmpSeqDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PAY_RPT_EMP_SEQ_DESC");
            entity.Property(e => e.PayGrpPayRptEmpSeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PAY_RPT_EMP_SEQ_NUM");
            entity.Property(e => e.PayGrpPayRptSeqDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PAY_RPT_SEQ_DESC");
            entity.Property(e => e.PayGrpPayRptSeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PAY_RPT_SEQ_NUM");
            entity.Property(e => e.PayGrpPayRptSubtotalCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PAY_RPT_SUBTOTAL_CD");
            entity.Property(e => e.PayGrpPayRptSubtotalDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PAY_RPT_SUBTOTAL_DESC");
            entity.Property(e => e.PayGrpProgErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_PROG_ERNGS_CD");
            entity.Property(e => e.PayGrpProrateHrlyCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PRORATE_HRLY_CD");
            entity.Property(e => e.PayGrpProrateHrlyDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PRORATE_HRLY_DESC");
            entity.Property(e => e.PayGrpProrateSlyrdCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PRORATE_SLYRD_CD");
            entity.Property(e => e.PayGrpProrateSlyrdDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PRORATE_SLYRD_DESC");
            entity.Property(e => e.PayGrpPyckAddrOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PYCK_ADDR_OPTN_DESC");
            entity.Property(e => e.PayGrpPyckAddrOptnInd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_ADDR_OPTN_IND");
            entity.Property(e => e.PayGrpPyckEmpSeqDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PYCK_EMP_SEQ_DESC");
            entity.Property(e => e.PayGrpPyckEmpSeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_EMP_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld01SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD01_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld02SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD02_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld03SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD03_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld04SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD04_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld05SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD05_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld06SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD06_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld07SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD07_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld08SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD08_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld09SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD09_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckFld10SeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_FLD10_SEQ_NUM");
            entity.Property(e => e.PayGrpPyckLocOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PYCK_LOC_OPTN_DESC");
            entity.Property(e => e.PayGrpPyckLocOptnInd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_LOC_OPTN_IND");
            entity.Property(e => e.PayGrpPyckSeqDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_PYCK_SEQ_DESC");
            entity.Property(e => e.PayGrpPyckSeqNum)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_PYCK_SEQ_NUM");
            entity.Property(e => e.PayGrpRetireePaygrpFlg)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_RETIREE_PAYGRP_FLG");
            entity.Property(e => e.PayGrpRetropayProgId)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_RETROPAY_PROG_ID");
            entity.Property(e => e.PayGrpRetropayTrgtProgId)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_RETROPAY_TRGT_PROG_ID");
            entity.Property(e => e.PayGrpRglrErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_RGLR_ERNGS_CD");
            entity.Property(e => e.PayGrpRglrHrsErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_RGLR_HRS_ERNGS_CD");
            entity.Property(e => e.PayGrpRtCnvrsnDtCd)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_RT_CNVRSN_DT_CD");
            entity.Property(e => e.PayGrpRtCnvrsnDtDesc)
                .HasMaxLength(30)
                .HasColumnName("PAY_GRP_RT_CNVRSN_DT_DESC");
            entity.Property(e => e.PayGrpRtTypeCd)
                .HasMaxLength(5)
                .HasColumnName("PAY_GRP_RT_TYPE_CD");
            entity.Property(e => e.PayGrpSrcBankId)
                .HasMaxLength(8)
                .HasColumnName("PAY_GRP_SRC_BANK_ID");
            entity.Property(e => e.PayGrpStdntFinPaygrpFlg)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_STDNT_FIN_PAYGRP_FLG");
            entity.Property(e => e.PayGrpTermProgId)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_TERM_PROG_ID");
            entity.Property(e => e.PayGrpTipsAdjstErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_TIPS_ADJST_ERNGS_CD");
            entity.Property(e => e.PayGrpTipsAdjstFlg)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_TIPS_ADJST_FLG");
            entity.Property(e => e.PayGrpTipsCredErngsCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_TIPS_CRED_ERNGS_CD");
            entity.Property(e => e.PayGrpTipsDelayWitheldFlg)
                .HasMaxLength(1)
                .HasColumnName("PAY_GRP_TIPS_DELAY_WITHELD_FLG");
            entity.Property(e => e.PayGrpWageLossPlanCd)
                .HasMaxLength(3)
                .HasColumnName("PAY_GRP_WAGE_LOSS_PLAN_CD");
            entity.Property(e => e.PayGrpWrkScheduleInd)
                .HasMaxLength(7)
                .HasColumnName("PAY_GRP_WRK_SCHEDULE_IND");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<PayItemNameDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PAY_ITEM_NAME_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.PinAutoAsgnTypeCd)
                .HasMaxLength(2)
                .HasColumnName("PIN_AUTO_ASGN_TYPE_CD");
            entity.Property(e => e.PinAutoAsgnTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_AUTO_ASGN_TYPE_DESC");
            entity.Property(e => e.PinCalOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_CAL_OVRD_IND");
            entity.Property(e => e.PinCd)
                .HasMaxLength(22)
                .HasColumnName("PIN_CD");
            entity.Property(e => e.PinClassCd)
                .HasMaxLength(2)
                .HasColumnName("PIN_CLASS_CD");
            entity.Property(e => e.PinClassDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_CLASS_DESC");
            entity.Property(e => e.PinCntryCd)
                .HasMaxLength(3)
                .HasColumnName("PIN_CNTRY_CD");
            entity.Property(e => e.PinCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_CNTRY_DESC");
            entity.Property(e => e.PinCtgyCd)
                .HasMaxLength(4)
                .HasColumnName("PIN_CTGY_CD");
            entity.Property(e => e.PinCustom1Nm)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM1_NM");
            entity.Property(e => e.PinCustom2Nm)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM2_NM");
            entity.Property(e => e.PinCustom3Nm)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM3_NM");
            entity.Property(e => e.PinCustom4Nm)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM4_NM");
            entity.Property(e => e.PinCustom5Nm)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM5_NM");
            entity.Property(e => e.PinDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_D_KEY");
            entity.Property(e => e.PinDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_DESC");
            entity.Property(e => e.PinDfntnAsofDtOptnCd)
                .HasMaxLength(1)
                .HasColumnName("PIN_DFNTN_ASOF_DT_OPTN_CD");
            entity.Property(e => e.PinDfntnAsofDtOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_DFNTN_ASOF_DT_OPTN_DESC");
            entity.Property(e => e.PinEffDt).HasColumnName("PIN_EFF_DT");
            entity.Property(e => e.PinEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("PIN_EFF_STAT_CD");
            entity.Property(e => e.PinElmntOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_ELMNT_OVRD_IND");
            entity.Property(e => e.PinExprDt).HasColumnName("PIN_EXPR_DT");
            entity.Property(e => e.PinFieldFrmtCd)
                .HasMaxLength(1)
                .HasColumnName("PIN_FIELD_FRMT_CD");
            entity.Property(e => e.PinFieldFrmtDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_FIELD_FRMT_DESC");
            entity.Property(e => e.PinIndstryCd)
                .HasMaxLength(4)
                .HasColumnName("PIN_INDSTRY_CD");
            entity.Property(e => e.PinLastUpdtdDttm).HasColumnName("PIN_LAST_UPDTD_DTTM");
            entity.Property(e => e.PinLastUpdtdUserId)
                .HasMaxLength(30)
                .HasColumnName("PIN_LAST_UPDTD_USER_ID");
            entity.Property(e => e.PinNm)
                .HasMaxLength(18)
                .HasColumnName("PIN_NM");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.PinOwnrCd)
                .HasMaxLength(1)
                .HasColumnName("PIN_OWNR_CD");
            entity.Property(e => e.PinOwnrDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_OWNR_DESC");
            entity.Property(e => e.PinParentNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_PARENT_NUM");
            entity.Property(e => e.PinPayEntlmntOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_PAY_ENTLMNT_OVRD_IND");
            entity.Property(e => e.PinPayGrpOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_PAY_GRP_OVRD_IND");
            entity.Property(e => e.PinPayeeOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_PAYEE_OVRD_IND");
            entity.Property(e => e.PinPostvInputOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_POSTV_INPUT_OVRD_IND");
            entity.Property(e => e.PinRecalcInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_RECALC_IND");
            entity.Property(e => e.PinStoreRsltIfZeroDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_STORE_RSLT_IF_ZERO_DESC");
            entity.Property(e => e.PinStoreRsltIfZeroInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_STORE_RSLT_IF_ZERO_IND");
            entity.Property(e => e.PinStoreRsltInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_STORE_RSLT_IND");
            entity.Property(e => e.PinSupportElmntOvrdInd)
                .HasMaxLength(1)
                .HasColumnName("PIN_SUPPORT_ELMNT_OVRD_IND");
            entity.Property(e => e.PinTypeCd)
                .HasMaxLength(2)
                .HasColumnName("PIN_TYPE_CD");
            entity.Property(e => e.PinUsedByCd)
                .HasMaxLength(1)
                .HasColumnName("PIN_USED_BY_CD");
            entity.Property(e => e.PinUsedByDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_USED_BY_DESC");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<PositionDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("POSITION_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.PosnActnCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_ACTN_CD");
            entity.Property(e => e.PosnActnDesc)
                .HasMaxLength(50)
                .HasColumnName("POSN_ACTN_DESC");
            entity.Property(e => e.PosnActnDt).HasColumnName("POSN_ACTN_DT");
            entity.Property(e => e.PosnActnRsnCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_ACTN_RSN_CD");
            entity.Property(e => e.PosnActnRsnDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_ACTN_RSN_DESC");
            entity.Property(e => e.PosnAddsToFteActlFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_ADDS_TO_FTE_ACTL_FLG");
            entity.Property(e => e.PosnAvlblTelewrkPosnFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_AVLBL_TELEWRK_POSN_FLG");
            entity.Property(e => e.PosnBargUnitCd)
                .HasMaxLength(4)
                .HasColumnName("POSN_BARG_UNIT_CD");
            entity.Property(e => e.PosnBargUnitDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_BARG_UNIT_DESC");
            entity.Property(e => e.PosnBdgtedFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_BDGTED_FLG");
            entity.Property(e => e.PosnBusUnitCd)
                .HasMaxLength(5)
                .HasColumnName("POSN_BUS_UNIT_CD");
            entity.Property(e => e.PosnBusUnitDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_BUS_UNIT_DESC");
            entity.Property(e => e.PosnClassDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_CLASS_DESC");
            entity.Property(e => e.PosnClassInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_CLASS_IND");
            entity.Property(e => e.PosnCmpyCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_CMPY_CD");
            entity.Property(e => e.PosnCmpyDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_CMPY_DESC");
            entity.Property(e => e.PosnCnfdntlFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_CNFDNTL_FLG");
            entity.Property(e => e.PosnCntryCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_CNTRY_CD");
            entity.Property(e => e.PosnCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_CNTRY_DESC");
            entity.Property(e => e.PosnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_D_KEY");
            entity.Property(e => e.PosnDeptCd)
                .HasMaxLength(10)
                .HasColumnName("POSN_DEPT_CD");
            entity.Property(e => e.PosnDeptDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_DEPT_DESC");
            entity.Property(e => e.PosnDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_DESC");
            entity.Property(e => e.PosnEduGovAcdmcRankDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_EDU_GOV_ACDMC_RANK_DESC");
            entity.Property(e => e.PosnEduGovAcdmcRankInd)
                .HasMaxLength(3)
                .HasColumnName("POSN_EDU_GOV_ACDMC_RANK_IND");
            entity.Property(e => e.PosnEduGovGrpInd)
                .HasMaxLength(6)
                .HasColumnName("POSN_EDU_GOV_GRP_IND");
            entity.Property(e => e.PosnEffDt).HasColumnName("POSN_EFF_DT");
            entity.Property(e => e.PosnEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("POSN_EFF_STAT_CD");
            entity.Property(e => e.PosnEmpRltnshpCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_EMP_RLTNSHP_CD");
            entity.Property(e => e.PosnEmpRltnshpDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_EMP_RLTNSHP_DESC");
            entity.Property(e => e.PosnEncmbrSlryAmt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("POSN_ENCMBR_SLRY_AMT");
            entity.Property(e => e.PosnEncmbrSlryOptnCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_ENCMBR_SLRY_OPTN_CD");
            entity.Property(e => e.PosnEncmbrSlryOptnDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_ENCMBR_SLRY_OPTN_DESC");
            entity.Property(e => e.PosnEncmbrncDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_ENCMBRNC_DESC");
            entity.Property(e => e.PosnEncmbrncInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_ENCMBRNC_IND");
            entity.Property(e => e.PosnFlsaStatCd)
                .HasMaxLength(1)
                .HasColumnName("POSN_FLSA_STAT_CD");
            entity.Property(e => e.PosnFlsaStatDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_FLSA_STAT_DESC");
            entity.Property(e => e.PosnFtePct)
                .HasColumnType("numeric(7, 2)")
                .HasColumnName("POSN_FTE_PCT");
            entity.Property(e => e.PosnFullPartDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_FULL_PART_DESC");
            entity.Property(e => e.PosnFullPartInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_FULL_PART_IND");
            entity.Property(e => e.PosnGrdCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_GRD_CD");
            entity.Property(e => e.PosnGrdCdDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_GRD_CD_DESC");
            entity.Property(e => e.PosnHlthCertDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_HLTH_CERT_DESC");
            entity.Property(e => e.PosnHlthCertFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_HLTH_CERT_FLG");
            entity.Property(e => e.PosnHrsFriQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_FRI_QTY");
            entity.Property(e => e.PosnHrsMonQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_MON_QTY");
            entity.Property(e => e.PosnHrsSatQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_SAT_QTY");
            entity.Property(e => e.PosnHrsSunQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_SUN_QTY");
            entity.Property(e => e.PosnHrsThuQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_THU_QTY");
            entity.Property(e => e.PosnHrsTueQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_TUE_QTY");
            entity.Property(e => e.PosnHrsWedQty)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("POSN_HRS_WED_QTY");
            entity.Property(e => e.PosnIncldSlryPlanFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_INCLD_SLRY_PLAN_FLG");
            entity.Property(e => e.PosnJobCd)
                .HasMaxLength(6)
                .HasColumnName("POSN_JOB_CD");
            entity.Property(e => e.PosnJobCdDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_JOB_CD_DESC");
            entity.Property(e => e.PosnJobShareFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_JOB_SHARE_FLG");
            entity.Property(e => e.PosnKeyDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_KEY_DESC");
            entity.Property(e => e.PosnKeyFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_KEY_FLG");
            entity.Property(e => e.PosnLangSkillCd)
                .HasMaxLength(2)
                .HasColumnName("POSN_LANG_SKILL_CD");
            entity.Property(e => e.PosnLangSkillDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_LANG_SKILL_DESC");
            entity.Property(e => e.PosnLastUpdtDt).HasColumnName("POSN_LAST_UPDT_DT");
            entity.Property(e => e.PosnLastUpdtLogonId)
                .HasMaxLength(30)
                .HasColumnName("POSN_LAST_UPDT_LOGON_ID");
            entity.Property(e => e.PosnLocCd)
                .HasMaxLength(10)
                .HasColumnName("POSN_LOC_CD");
            entity.Property(e => e.PosnLocDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_LOC_DESC");
            entity.Property(e => e.PosnMailDropCd)
                .HasMaxLength(50)
                .HasColumnName("POSN_MAIL_DROP_CD");
            entity.Property(e => e.PosnMaxHeadCountQty)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_MAX_HEAD_COUNT_QTY");
            entity.Property(e => e.PosnMgrLvlCd)
                .HasMaxLength(2)
                .HasColumnName("POSN_MGR_LVL_CD");
            entity.Property(e => e.PosnMgrLvlDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_MGR_LVL_DESC");
            entity.Property(e => e.PosnNum)
                .HasMaxLength(8)
                .HasColumnName("POSN_NUM");
            entity.Property(e => e.PosnOrgCd)
                .HasMaxLength(60)
                .HasColumnName("POSN_ORG_CD");
            entity.Property(e => e.PosnOrgInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_ORG_IND");
            entity.Property(e => e.PosnPhNum)
                .HasMaxLength(24)
                .HasColumnName("POSN_PH_NUM");
            entity.Property(e => e.PosnPoolId)
                .HasMaxLength(3)
                .HasColumnName("POSN_POOL_ID");
            entity.Property(e => e.PosnPoolIdDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_POOL_ID_DESC");
            entity.Property(e => e.PosnRepCd)
                .HasMaxLength(1)
                .HasColumnName("POSN_REP_CD");
            entity.Property(e => e.PosnRepDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_REP_DESC");
            entity.Property(e => e.PosnRglrTempDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_RGLR_TEMP_DESC");
            entity.Property(e => e.PosnRglrTempInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_RGLR_TEMP_IND");
            entity.Property(e => e.PosnRgltryRegnCd)
                .HasMaxLength(5)
                .HasColumnName("POSN_RGLTRY_REGN_CD");
            entity.Property(e => e.PosnRgltryRegnDesc)
                .HasMaxLength(50)
                .HasColumnName("POSN_RGLTRY_REGN_DESC");
            entity.Property(e => e.PosnRptDottedLineNm)
                .HasMaxLength(8)
                .HasColumnName("POSN_RPT_DOTTED_LINE_NM");
            entity.Property(e => e.PosnRptsToNm)
                .HasMaxLength(8)
                .HasColumnName("POSN_RPTS_TO_NM");
            entity.Property(e => e.PosnScrtyClrncTypeCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_SCRTY_CLRNC_TYPE_CD");
            entity.Property(e => e.PosnScrtyClrncTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_SCRTY_CLRNC_TYPE_DESC");
            entity.Property(e => e.PosnSeasonlInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_SEASONL_IND");
            entity.Property(e => e.PosnSgntrAuthtyDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_SGNTR_AUTHTY_DESC");
            entity.Property(e => e.PosnSgntrAuthtyFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_SGNTR_AUTHTY_FLG");
            entity.Property(e => e.PosnShiftDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_SHIFT_DESC");
            entity.Property(e => e.PosnShiftInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_SHIFT_IND");
            entity.Property(e => e.PosnSlryAdminPlanCd)
                .HasMaxLength(4)
                .HasColumnName("POSN_SLRY_ADMIN_PLAN_CD");
            entity.Property(e => e.PosnSlryAdminPlanDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_SLRY_ADMIN_PLAN_DESC");
            entity.Property(e => e.PosnSpclTrngCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_SPCL_TRNG_CD");
            entity.Property(e => e.PosnSrcId)
                .HasMaxLength(30)
                .HasColumnName("POSN_SRC_ID");
            entity.Property(e => e.PosnStatCd)
                .HasMaxLength(1)
                .HasColumnName("POSN_STAT_CD");
            entity.Property(e => e.PosnStatDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_STAT_DESC");
            entity.Property(e => e.PosnStatDt).HasColumnName("POSN_STAT_DT");
            entity.Property(e => e.PosnStdHrsFreqCd)
                .HasMaxLength(5)
                .HasColumnName("POSN_STD_HRS_FREQ_CD");
            entity.Property(e => e.PosnStdHrsFreqDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_STD_HRS_FREQ_DESC");
            entity.Property(e => e.PosnStdHrsQty)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("POSN_STD_HRS_QTY");
            entity.Property(e => e.PosnStepNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_STEP_NUM");
            entity.Property(e => e.PosnSupvsryLvlCd)
                .HasMaxLength(8)
                .HasColumnName("POSN_SUPVSRY_LVL_CD");
            entity.Property(e => e.PosnSupvsryLvlDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_SUPVSRY_LVL_DESC");
            entity.Property(e => e.PosnSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("POSN__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PosnSvmSeqMrf).HasColumnName("POSN__SVM_SEQ_MRF");
            entity.Property(e => e.PosnSvmSeqNum).HasColumnName("POSN__SVM_SEQ_NUM");
            entity.Property(e => e.PosnTrngProgCd)
                .HasMaxLength(6)
                .HasColumnName("POSN_TRNG_PROG_CD");
            entity.Property(e => e.PosnTrngProgDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_TRNG_PROG_DESC");
            entity.Property(e => e.PosnUcFteJobPosnSameFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_UC_FTE_JOB_POSN_SAME_FLG");
            entity.Property(e => e.PosnUcHrGrp)
                .HasMaxLength(30)
                .HasColumnName("POSN_UC_HR_GRP");
            entity.Property(e => e.PosnUnionCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_UNION_CD");
            entity.Property(e => e.PosnUnionDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_UNION_DESC");
            entity.Property(e => e.PosnUpdtIncmbntFlg)
                .HasMaxLength(1)
                .HasColumnName("POSN_UPDT_INCMBNT_FLG");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<PrimaryJobsDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PRIMARY_JOBS_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.PrmyJobsDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRMY_JOBS_D_KEY");
            entity.Property(e => e.PrmyJobsEffDt).HasColumnName("PRMY_JOBS_EFF_DT");
            entity.Property(e => e.PrmyJobsEmpId)
                .HasMaxLength(11)
                .HasColumnName("PRMY_JOBS_EMP_ID");
            entity.Property(e => e.PrmyJobsEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRMY_JOBS_EMP_REC_NUM");
            entity.Property(e => e.PrmyJobsExprDt).HasColumnName("PRMY_JOBS_EXPR_DT");
            entity.Property(e => e.PrmyJobsJobEffSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRMY_JOBS_JOB_EFF_SEQ_NUM");
            entity.Property(e => e.PrmyJobsJobEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PRMY_JOBS_JOB_EMP_REC_NUM");
            entity.Property(e => e.PrmyJobsJobFlg)
                .HasMaxLength(1)
                .HasColumnName("PRMY_JOBS_JOB_FLG");
            entity.Property(e => e.PrmyJobsPrmy1Flg)
                .HasMaxLength(1)
                .HasColumnName("PRMY_JOBS_PRMY1_FLG");
            entity.Property(e => e.PrmyJobsPrmy2Flg)
                .HasMaxLength(1)
                .HasColumnName("PRMY_JOBS_PRMY2_FLG");
            entity.Property(e => e.PrmyJobsPrmyJobAplctnCd)
                .HasMaxLength(2)
                .HasColumnName("PRMY_JOBS_PRMY_JOB_APLCTN_CD");
            entity.Property(e => e.PrmyJobsPrmyJobAplctnDesc)
                .HasMaxLength(30)
                .HasColumnName("PRMY_JOBS_PRMY_JOB_APLCTN_DESC");
            entity.Property(e => e.PrmyJobsSrcCd)
                .HasMaxLength(1)
                .HasColumnName("PRMY_JOBS_SRC_CD");
            entity.Property(e => e.PrmyJobsSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PRMY_JOBS__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PrmyJobsSvmSeqMrf).HasColumnName("PRMY_JOBS__SVM_SEQ_MRF");
            entity.Property(e => e.PrmyJobsSvmSeqNum).HasColumnName("PRMY_JOBS__SVM_SEQ_NUM");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<Prj2EmpidToPpsid>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PRJ2_EMPID_TO_PPSID");

            entity.Property(e => e.Column10)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 10");
            entity.Property(e => e.Column11)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 11");
            entity.Property(e => e.Column12)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 12");
            entity.Property(e => e.Column13)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 13");
            entity.Property(e => e.Column3)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 3");
            entity.Property(e => e.Column4)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 4");
            entity.Property(e => e.Column5)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 5");
            entity.Property(e => e.Column6)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 6");
            entity.Property(e => e.Column7)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 7");
            entity.Property(e => e.Column8)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 8");
            entity.Property(e => e.Column9)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Column 9");
            entity.Property(e => e.Emplid)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMPLID");
            entity.Property(e => e.PpsId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PPS_ID");
            entity.Property(e => e.UcpathName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UCPath_NAME");
        });

        modelBuilder.Entity<PsAcctCdTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ACCT_CD_TBL_V");

            entity.Property(e => e.Account)
                .HasMaxLength(10)
                .HasColumnName("ACCOUNT");
            entity.Property(e => e.AcctCd)
                .HasMaxLength(25)
                .HasColumnName("ACCT_CD");
            entity.Property(e => e.ActivityId)
                .HasMaxLength(15)
                .HasColumnName("ACTIVITY_ID");
            entity.Property(e => e.Affiliate)
                .HasMaxLength(5)
                .HasColumnName("AFFILIATE");
            entity.Property(e => e.AffiliateIntra1)
                .HasMaxLength(10)
                .HasColumnName("AFFILIATE_INTRA1");
            entity.Property(e => e.AffiliateIntra2)
                .HasMaxLength(10)
                .HasColumnName("AFFILIATE_INTRA2");
            entity.Property(e => e.Altacct)
                .HasMaxLength(10)
                .HasColumnName("ALTACCT");
            entity.Property(e => e.BudgetRef)
                .HasMaxLength(8)
                .HasColumnName("BUDGET_REF");
            entity.Property(e => e.BusinessUnitPc)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT_PC");
            entity.Property(e => e.Chartfield1)
                .HasMaxLength(10)
                .HasColumnName("CHARTFIELD1");
            entity.Property(e => e.Chartfield2)
                .HasMaxLength(10)
                .HasColumnName("CHARTFIELD2");
            entity.Property(e => e.Chartfield3)
                .HasMaxLength(10)
                .HasColumnName("CHARTFIELD3");
            entity.Property(e => e.ClassFld)
                .HasMaxLength(5)
                .HasColumnName("CLASS_FLD");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DeptidCf)
                .HasMaxLength(10)
                .HasColumnName("DEPTID_CF");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DirectCharge)
                .HasMaxLength(1)
                .HasColumnName("DIRECT_CHARGE");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EncumbAccount)
                .HasMaxLength(10)
                .HasColumnName("ENCUMB_ACCOUNT");
            entity.Property(e => e.FdmHash)
                .HasMaxLength(31)
                .HasColumnName("FDM_HASH");
            entity.Property(e => e.FundCode)
                .HasMaxLength(5)
                .HasColumnName("FUND_CODE");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OperatingUnit)
                .HasMaxLength(8)
                .HasColumnName("OPERATING_UNIT");
            entity.Property(e => e.PreEncumbAccount)
                .HasMaxLength(10)
                .HasColumnName("PRE_ENCUMB_ACCOUNT");
            entity.Property(e => e.Product)
                .HasMaxLength(6)
                .HasColumnName("PRODUCT");
            entity.Property(e => e.ProgramCode)
                .HasMaxLength(5)
                .HasColumnName("PROGRAM_CODE");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(15)
                .HasColumnName("PROJECT_ID");
            entity.Property(e => e.ProrateLiability)
                .HasMaxLength(1)
                .HasColumnName("PRORATE_LIABILITY");
            entity.Property(e => e.ResourceCategory)
                .HasMaxLength(5)
                .HasColumnName("RESOURCE_CATEGORY");
            entity.Property(e => e.ResourceSubCat)
                .HasMaxLength(5)
                .HasColumnName("RESOURCE_SUB_CAT");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(5)
                .HasColumnName("RESOURCE_TYPE");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsActionTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ACTION_TBL_V");

            entity.HasIndex(e => e.Action, "ucpathods_ps_action_key");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDescr)
                .HasMaxLength(50)
                .HasColumnName("ACTION_DESCR");
            entity.Property(e => e.ActionDescrshort)
                .HasMaxLength(30)
                .HasColumnName("ACTION_DESCRSHORT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.HrActionType)
                .HasMaxLength(1)
                .HasColumnName("HR_ACTION_TYPE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.Objectownerid)
                .HasMaxLength(4)
                .HasColumnName("OBJECTOWNERID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PsActionSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PS_ACTION__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsActionSvmSeqMrf).HasColumnName("PS_ACTION__SVM_SEQ_MRF");
            entity.Property(e => e.PsActionSvmSeqNum).HasColumnName("PS_ACTION__SVM_SEQ_NUM");
            entity.Property(e => e.SystemDataFlg)
                .HasMaxLength(1)
                .HasColumnName("SYSTEM_DATA_FLG");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsActnReasonTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ACTN_REASON_TBL_V");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionReason)
                .HasMaxLength(3)
                .HasColumnName("ACTION_REASON");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.HrActionType)
                .HasMaxLength(1)
                .HasColumnName("HR_ACTION_TYPE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.Objectownerid)
                .HasMaxLength(4)
                .HasColumnName("OBJECTOWNERID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PsActnRsnSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PS_ACTN_RSN__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsActnRsnSvmSeqMrf).HasColumnName("PS_ACTN_RSN__SVM_SEQ_MRF");
            entity.Property(e => e.PsActnRsnSvmSeqNum).HasColumnName("PS_ACTN_RSN__SVM_SEQ_NUM");
            entity.Property(e => e.SystemDataFlg)
                .HasMaxLength(1)
                .HasColumnName("SYSTEM_DATA_FLG");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsAddlPayDataV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ADDL_PAY_DATA_V");

            entity.Property(e => e.AcctCd)
                .HasMaxLength(25)
                .HasColumnName("ACCT_CD");
            entity.Property(e => e.AddlPayFrequency)
                .HasMaxLength(1)
                .HasColumnName("ADDL_PAY_FREQUENCY");
            entity.Property(e => e.AddlPayShift)
                .HasMaxLength(1)
                .HasColumnName("ADDL_PAY_SHIFT");
            entity.Property(e => e.AddlSeq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ADDL_SEQ");
            entity.Property(e => e.AddlpayReason)
                .HasMaxLength(3)
                .HasColumnName("ADDLPAY_REASON");
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.CompRatecd)
                .HasMaxLength(6)
                .HasColumnName("COMP_RATECD");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DedSubsetGenl)
                .HasMaxLength(3)
                .HasColumnName("DED_SUBSET_GENL");
            entity.Property(e => e.DedSubsetId)
                .HasMaxLength(3)
                .HasColumnName("DED_SUBSET_ID");
            entity.Property(e => e.DedTaken)
                .HasMaxLength(1)
                .HasColumnName("DED_TAKEN");
            entity.Property(e => e.DedTakenGenl)
                .HasMaxLength(1)
                .HasColumnName("DED_TAKEN_GENL");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DisableDirDep)
                .HasMaxLength(1)
                .HasColumnName("DISABLE_DIR_DEP");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EarningsEndDt).HasColumnName("EARNINGS_END_DT");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Erncd)
                .HasMaxLength(3)
                .HasColumnName("ERNCD");
            entity.Property(e => e.GlPayType)
                .HasMaxLength(6)
                .HasColumnName("GL_PAY_TYPE");
            entity.Property(e => e.GoalAmt)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("GOAL_AMT");
            entity.Property(e => e.GoalBal)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("GOAL_BAL");
            entity.Property(e => e.HourlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("HOURLY_RT");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.Locality)
                .HasMaxLength(10)
                .HasColumnName("LOCALITY");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OkToPay)
                .HasMaxLength(1)
                .HasColumnName("OK_TO_PAY");
            entity.Property(e => e.OthHrs)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("OTH_HRS");
            entity.Property(e => e.OthPay)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("OTH_PAY");
            entity.Property(e => e.PayPeriod1)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD1");
            entity.Property(e => e.PayPeriod2)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD2");
            entity.Property(e => e.PayPeriod3)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD3");
            entity.Property(e => e.PayPeriod4)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD4");
            entity.Property(e => e.PayPeriod5)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD5");
            entity.Property(e => e.PlanType)
                .HasMaxLength(2)
                .HasColumnName("PLAN_TYPE");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.ProrateAddlPay)
                .HasMaxLength(1)
                .HasColumnName("PRORATE_ADDL_PAY");
            entity.Property(e => e.ProrateCuiWeeks)
                .HasMaxLength(1)
                .HasColumnName("PRORATE_CUI_WEEKS");
            entity.Property(e => e.PsAddlPaySvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PS_ADDL_PAY__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsAddlPaySvmSeqMrf).HasColumnName("PS_ADDL_PAY__SVM_SEQ_MRF");
            entity.Property(e => e.PsAddlPaySvmSeqNum).HasColumnName("PS_ADDL_PAY__SVM_SEQ_NUM");
            entity.Property(e => e.RecordSource)
                .HasMaxLength(1)
                .HasColumnName("RECORD_SOURCE");
            entity.Property(e => e.Sepchk)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SEPCHK");
            entity.Property(e => e.State)
                .HasMaxLength(6)
                .HasColumnName("STATE");
            entity.Property(e => e.TaxMethod)
                .HasMaxLength(1)
                .HasColumnName("TAX_METHOD");
            entity.Property(e => e.TaxPeriods)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("TAX_PERIODS");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsAddressTypTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ADDRESS_TYP_TBL_V");

            entity.Property(e => e.AddrTypeDescr)
                .HasMaxLength(30)
                .HasColumnName("ADDR_TYPE_DESCR");
            entity.Property(e => e.AddrTypeShort)
                .HasMaxLength(15)
                .HasColumnName("ADDR_TYPE_SHORT");
            entity.Property(e => e.AddressType)
                .HasMaxLength(4)
                .HasColumnName("ADDRESS_TYPE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DataTypeCd)
                .HasMaxLength(1)
                .HasColumnName("DATA_TYPE_CD");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OrderBySeq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORDER_BY_SEQ");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsAddressesV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ADDRESSES_V");

            entity.Property(e => e.Address1)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS1");
            entity.Property(e => e.Address2)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS2");
            entity.Property(e => e.Address3)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS3");
            entity.Property(e => e.Address4)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS4");
            entity.Property(e => e.AddressSvmPrimary)
                .HasMaxLength(2)
                .HasColumnName("ADDRESS__SVM_PRIMARY");
            entity.Property(e => e.AddressSvmSeqMrf).HasColumnName("ADDRESS__SVM_SEQ_MRF");
            entity.Property(e => e.AddressSvmSeqNum).HasColumnName("ADDRESS__SVM_SEQ_NUM");
            entity.Property(e => e.AddressType)
                .HasMaxLength(4)
                .HasColumnName("ADDRESS_TYPE");
            entity.Property(e => e.City)
                .HasMaxLength(30)
                .HasColumnName("CITY");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.County)
                .HasMaxLength(30)
                .HasColumnName("COUNTY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.HouseType)
                .HasMaxLength(2)
                .HasColumnName("HOUSE_TYPE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Postal)
                .HasMaxLength(12)
                .HasColumnName("POSTAL");
            entity.Property(e => e.RegRegion)
                .HasMaxLength(5)
                .HasColumnName("REG_REGION");
            entity.Property(e => e.State)
                .HasMaxLength(6)
                .HasColumnName("STATE");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsCitizenStsTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_CITIZEN_STS_TBL_V");

            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsCitizenshipV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_CITIZENSHIP_V");

            entity.HasIndex(e => e.Emplid, "ucpathods_ps_citizenship_emplid");

            entity.Property(e => e.CitizenSvmPrimary)
                .HasMaxLength(2)
                .HasColumnName("CITIZEN__SVM_PRIMARY");
            entity.Property(e => e.CitizenSvmSeqMrf).HasColumnName("CITIZEN__SVM_SEQ_MRF");
            entity.Property(e => e.CitizenSvmSeqNum).HasColumnName("CITIZEN__SVM_SEQ_NUM");
            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DependentId)
                .HasMaxLength(2)
                .HasColumnName("DEPENDENT_ID");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsCompRatecdTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_COMP_RATECD_TBL_V");

            entity.Property(e => e.CmpCalcBy)
                .HasMaxLength(2)
                .HasColumnName("CMP_CALC_BY");
            entity.Property(e => e.CmpNonUpdInd)
                .HasMaxLength(1)
                .HasColumnName("CMP_NON_UPD_IND");
            entity.Property(e => e.CmpPayableSw)
                .HasMaxLength(1)
                .HasColumnName("CMP_PAYABLE_SW");
            entity.Property(e => e.CompBasePaySw)
                .HasMaxLength(1)
                .HasColumnName("COMP_BASE_PAY_SW");
            entity.Property(e => e.CompFrequency)
                .HasMaxLength(5)
                .HasColumnName("COMP_FREQUENCY");
            entity.Property(e => e.CompPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("COMP_PCT");
            entity.Property(e => e.CompRateType)
                .HasMaxLength(2)
                .HasColumnName("COMP_RATE_TYPE");
            entity.Property(e => e.CompRatecd)
                .HasMaxLength(6)
                .HasColumnName("COMP_RATECD");
            entity.Property(e => e.Comprate)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("COMPRATE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.CurrencyCd)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Erncd)
                .HasMaxLength(3)
                .HasColumnName("ERNCD");
            entity.Property(e => e.FteIndicator)
                .HasMaxLength(1)
                .HasColumnName("FTE_INDICATOR");
            entity.Property(e => e.LookupId)
                .HasMaxLength(15)
                .HasColumnName("LOOKUP_ID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.RateCodeClass)
                .HasMaxLength(6)
                .HasColumnName("RATE_CODE_CLASS");
            entity.Property(e => e.SalPkgWarn)
                .HasMaxLength(1)
                .HasColumnName("SAL_PKG_WARN");
            entity.Property(e => e.SeniorityCalc)
                .HasMaxLength(1)
                .HasColumnName("SENIORITY_CALC");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UseHighestRtSw)
                .HasMaxLength(1)
                .HasColumnName("USE_HIGHEST_RT_SW");
        });

        modelBuilder.Entity<PsCompensationV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_COMPENSATION_V");

            entity.Property(e => e.ChangeAmt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CHANGE_AMT");
            entity.Property(e => e.ChangePct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("CHANGE_PCT");
            entity.Property(e => e.ChangePts)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CHANGE_PTS");
            entity.Property(e => e.CmpSrcInd)
                .HasMaxLength(1)
                .HasColumnName("CMP_SRC_IND");
            entity.Property(e => e.CompEffseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_EFFSEQ");
            entity.Property(e => e.CompFrequency)
                .HasMaxLength(5)
                .HasColumnName("COMP_FREQUENCY");
            entity.Property(e => e.CompPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("COMP_PCT");
            entity.Property(e => e.CompRatePoints)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("COMP_RATE_POINTS");
            entity.Property(e => e.CompRatecd)
                .HasMaxLength(6)
                .HasColumnName("COMP_RATECD");
            entity.Property(e => e.Comprate)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("COMPRATE");
            entity.Property(e => e.ConvertComprt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CONVERT_COMPRT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.CurrencyCd)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFFSEQ");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.FteIndicator)
                .HasMaxLength(1)
                .HasColumnName("FTE_INDICATOR");
            entity.Property(e => e.ManualSw)
                .HasMaxLength(1)
                .HasColumnName("MANUAL_SW");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.RateCodeGroup)
                .HasMaxLength(6)
                .HasColumnName("RATE_CODE_GROUP");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsCountryTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_COUNTRY_TBL_V");

            entity.Property(e => e.AddrValidat)
                .HasMaxLength(1)
                .HasColumnName("ADDR_VALIDAT");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.Country2char)
                .HasMaxLength(2)
                .HasColumnName("COUNTRY_2CHAR");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EoAddrValClass)
                .HasMaxLength(30)
                .HasColumnName("EO_ADDR_VAL_CLASS");
            entity.Property(e => e.EoAddrValMethod)
                .HasMaxLength(32)
                .HasColumnName("EO_ADDR_VAL_METHOD");
            entity.Property(e => e.EoAddrValPath)
                .HasMaxLength(254)
                .HasColumnName("EO_ADDR_VAL_PATH");
            entity.Property(e => e.EoSecPageName)
                .HasMaxLength(18)
                .HasColumnName("EO_SEC_PAGE_NAME");
            entity.Property(e => e.EuMemberState)
                .HasMaxLength(1)
                .HasColumnName("EU_MEMBER_STATE");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PostSrchAvail)
                .HasMaxLength(1)
                .HasColumnName("POST_SRCH_AVAIL");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsDeptBudgetDtV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_DEPT_BUDGET_DT_V");

            entity.Property(e => e.AcctCdDed)
                .HasMaxLength(25)
                .HasColumnName("ACCT_CD_DED");
            entity.Property(e => e.AcctCdTax)
                .HasMaxLength(25)
                .HasColumnName("ACCT_CD_TAX");
            entity.Property(e => e.BudgetBeginDt).HasColumnName("BUDGET_BEGIN_DT");
            entity.Property(e => e.BudgetCapIndc)
                .HasMaxLength(1)
                .HasColumnName("BUDGET_CAP_INDC");
            entity.Property(e => e.BudgetEndDt).HasColumnName("BUDGET_END_DT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DefaultFundOptn)
                .HasMaxLength(1)
                .HasColumnName("DEFAULT_FUND_OPTN");
            entity.Property(e => e.DeptOffsetGrp)
                .HasMaxLength(5)
                .HasColumnName("DEPT_OFFSET_GRP");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.FiscalYear)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FISCAL_YEAR");
            entity.Property(e => e.FundEndDtDed).HasColumnName("FUND_END_DT_DED");
            entity.Property(e => e.FundEndDtTax).HasColumnName("FUND_END_DT_TAX");
            entity.Property(e => e.HpDedAcct)
                .HasMaxLength(1)
                .HasColumnName("HP_DED_ACCT");
            entity.Property(e => e.HpDfltFndDtFlg)
                .HasMaxLength(1)
                .HasColumnName("HP_DFLT_FND_DT_FLG");
            entity.Property(e => e.HpErnAcct)
                .HasMaxLength(1)
                .HasColumnName("HP_ERN_ACCT");
            entity.Property(e => e.HpFringeGroup)
                .HasMaxLength(5)
                .HasColumnName("HP_FRINGE_GROUP");
            entity.Property(e => e.HpTaxAcct)
                .HasMaxLength(1)
                .HasColumnName("HP_TAX_ACCT");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsDeptBudgetErnV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_DEPT_BUDGET_ERN_V");

            entity.HasIndex(e => e.PositionNbr, "ucpathods_ps_dept_budget_ern_position_nbr");

            entity.Property(e => e.AcctCd)
                .HasMaxLength(25)
                .HasColumnName("ACCT_CD");
            entity.Property(e => e.BudgetAmt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("BUDGET_AMT");
            entity.Property(e => e.BudgetSeq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BUDGET_SEQ");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DistPct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("DIST_PCT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFFSEQ");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Erncd)
                .HasMaxLength(3)
                .HasColumnName("ERNCD");
            entity.Property(e => e.FiscalYear)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FISCAL_YEAR");
            entity.Property(e => e.FundingEndDt).HasColumnName("FUNDING_END_DT");
            entity.Property(e => e.GlPayType)
                .HasMaxLength(6)
                .HasColumnName("GL_PAY_TYPE");
            entity.Property(e => e.HpExcess)
                .HasMaxLength(1)
                .HasColumnName("HP_EXCESS");
            entity.Property(e => e.HpFringeGroup)
                .HasMaxLength(5)
                .HasColumnName("HP_FRINGE_GROUP");
            entity.Property(e => e.HpRedirectAcct)
                .HasMaxLength(25)
                .HasColumnName("HP_REDIRECT_ACCT");
            entity.Property(e => e.HpUsedDistributn)
                .HasMaxLength(1)
                .HasColumnName("HP_USED_DISTRIBUTN");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.MonthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("MONTHLY_RT");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PercentEffort)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("PERCENT_EFFORT");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PositionPoolId)
                .HasMaxLength(3)
                .HasColumnName("POSITION_POOL_ID");
            entity.Property(e => e.PsDeptBudgetErnSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PS_DEPT_BUDGET_ERN__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsDeptBudgetErnSvmSeqMrf).HasColumnName("PS_DEPT_BUDGET_ERN__SVM_SEQ_MRF");
            entity.Property(e => e.PsDeptBudgetErnSvmSeqNum).HasColumnName("PS_DEPT_BUDGET_ERN__SVM_SEQ_NUM");
            entity.Property(e => e.RequestId)
                .HasMaxLength(10)
                .HasColumnName("REQUEST_ID");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.SetidJobcode)
                .HasMaxLength(5)
                .HasColumnName("SETID_JOBCODE");
            entity.Property(e => e.UcCaprate)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("UC_CAPRATE");
            entity.Property(e => e.UcCaprateFteYr)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_CAPRATE_FTE_YR");
            entity.Property(e => e.UcCaptype)
                .HasMaxLength(3)
                .HasColumnName("UC_CAPTYPE");
            entity.Property(e => e.UcPercentEffort)
                .HasColumnType("numeric(9, 6)")
                .HasColumnName("UC_PERCENT_EFFORT");
            entity.Property(e => e.UcPercentPay)
                .HasColumnType("numeric(9, 6)")
                .HasColumnName("UC_PERCENT_PAY");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsDeptTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_DEPT_TBL_V");

            entity.Property(e => e.AccountingOwner)
                .HasMaxLength(30)
                .HasColumnName("ACCOUNTING_OWNER");
            entity.Property(e => e.BudgetDeptid)
                .HasMaxLength(10)
                .HasColumnName("BUDGET_DEPTID");
            entity.Property(e => e.BudgetLvl)
                .HasMaxLength(1)
                .HasColumnName("BUDGET_LVL");
            entity.Property(e => e.BudgetYrEndDt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BUDGET_YR_END_DT");
            entity.Property(e => e.Company)
                .HasMaxLength(3)
                .HasColumnName("COMPANY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DeptTenureFlg)
                .HasMaxLength(1)
                .HasColumnName("DEPT_TENURE_FLG");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Eeo4Function)
                .HasMaxLength(2)
                .HasColumnName("EEO4_FUNCTION");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Estabid)
                .HasMaxLength(12)
                .HasColumnName("ESTABID");
            entity.Property(e => e.FteEditIndc)
                .HasMaxLength(1)
                .HasColumnName("FTE_EDIT_INDC");
            entity.Property(e => e.GlExpense)
                .HasMaxLength(35)
                .HasColumnName("GL_EXPENSE");
            entity.Property(e => e.Location)
                .HasMaxLength(10)
                .HasColumnName("LOCATION");
            entity.Property(e => e.ManagerId)
                .HasMaxLength(11)
                .HasColumnName("MANAGER_ID");
            entity.Property(e => e.ManagerName)
                .HasMaxLength(30)
                .HasColumnName("MANAGER_NAME");
            entity.Property(e => e.ManagerPosn)
                .HasMaxLength(8)
                .HasColumnName("MANAGER_POSN");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Riskcd)
                .HasMaxLength(6)
                .HasColumnName("RISKCD");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.SetidLocation)
                .HasMaxLength(5)
                .HasColumnName("SETID_LOCATION");
            entity.Property(e => e.TaxLocationCd)
                .HasMaxLength(10)
                .HasColumnName("TAX_LOCATION_CD");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UseBudgets)
                .HasMaxLength(1)
                .HasColumnName("USE_BUDGETS");
            entity.Property(e => e.UseDistribution)
                .HasMaxLength(1)
                .HasColumnName("USE_DISTRIBUTION");
            entity.Property(e => e.UseEncumbrances)
                .HasMaxLength(1)
                .HasColumnName("USE_ENCUMBRANCES");
        });

        modelBuilder.Entity<PsDiversEthnicV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_DIVERS_ETHNIC_V");

            entity.Property(e => e.ApsEcNdsAus)
                .HasMaxLength(1)
                .HasColumnName("APS_EC_NDS_AUS");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EthnicGrpCd)
                .HasMaxLength(8)
                .HasColumnName("ETHNIC_GRP_CD");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PrimaryIndicator)
                .HasMaxLength(1)
                .HasColumnName("PRIMARY_INDICATOR");
            entity.Property(e => e.RegRegion)
                .HasMaxLength(5)
                .HasColumnName("REG_REGION");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsEarningsBalV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_EARNINGS_BAL_V");

            entity.Property(e => e.BalanceId)
                .HasMaxLength(2)
                .HasColumnName("BALANCE_ID");
            entity.Property(e => e.BalancePeriod)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BALANCE_PERIOD");
            entity.Property(e => e.BalanceQtr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BALANCE_QTR");
            entity.Property(e => e.BalanceYear)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("BALANCE_YEAR");
            entity.Property(e => e.Company)
                .HasMaxLength(3)
                .HasColumnName("COMPANY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Erncd)
                .HasMaxLength(3)
                .HasColumnName("ERNCD");
            entity.Property(e => e.GrsMtd)
                .HasColumnType("numeric(13, 2)")
                .HasColumnName("GRS_MTD");
            entity.Property(e => e.GrsQtd)
                .HasColumnType("numeric(13, 2)")
                .HasColumnName("GRS_QTD");
            entity.Property(e => e.GrsYtd)
                .HasColumnType("numeric(13, 2)")
                .HasColumnName("GRS_YTD");
            entity.Property(e => e.HrsMtd)
                .HasColumnType("numeric(8, 2)")
                .HasColumnName("HRS_MTD");
            entity.Property(e => e.HrsQtd)
                .HasColumnType("numeric(8, 2)")
                .HasColumnName("HRS_QTD");
            entity.Property(e => e.HrsYtd)
                .HasColumnType("numeric(8, 2)")
                .HasColumnName("HRS_YTD");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.SpclBalance)
                .HasMaxLength(1)
                .HasColumnName("SPCL_BALANCE");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsEarningsTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_EARNINGS_TBL_V");

            entity.Property(e => e.AddDe)
                .HasMaxLength(1)
                .HasColumnName("ADD_DE");
            entity.Property(e => e.AddGross)
                .HasMaxLength(1)
                .HasColumnName("ADD_GROSS");
            entity.Property(e => e.AllowEmpltype)
                .HasMaxLength(1)
                .HasColumnName("ALLOW_EMPLTYPE");
            entity.Property(e => e.AmtOrHours)
                .HasMaxLength(1)
                .HasColumnName("AMT_OR_HOURS");
            entity.Property(e => e.BasedOnAccErncd)
                .HasMaxLength(3)
                .HasColumnName("BASED_ON_ACC_ERNCD");
            entity.Property(e => e.BasedOnErncd)
                .HasMaxLength(3)
                .HasColumnName("BASED_ON_ERNCD");
            entity.Property(e => e.BasedOnType)
                .HasMaxLength(1)
                .HasColumnName("BASED_ON_TYPE");
            entity.Property(e => e.BudgetEffect)
                .HasMaxLength(1)
                .HasColumnName("BUDGET_EFFECT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DedcdPayback)
                .HasMaxLength(6)
                .HasColumnName("DEDCD_PAYBACK");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EarnFlatAmt)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("EARN_FLAT_AMT");
            entity.Property(e => e.EarnYtdMax)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("EARN_YTD_MAX");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EffectOnFlsa)
                .HasMaxLength(1)
                .HasColumnName("EFFECT_ON_FLSA");
            entity.Property(e => e.EligForRetropay)
                .HasMaxLength(1)
                .HasColumnName("ELIG_FOR_RETROPAY");
            entity.Property(e => e.ErnSequence)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ERN_SEQUENCE");
            entity.Property(e => e.Erncd)
                .HasMaxLength(3)
                .HasColumnName("ERNCD");
            entity.Property(e => e.FactorErnAdj)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("FACTOR_ERN_ADJ");
            entity.Property(e => e.FactorHrsAdj)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("FACTOR_HRS_ADJ");
            entity.Property(e => e.FactorMult)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("FACTOR_MULT");
            entity.Property(e => e.FactorRateAdj)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("FACTOR_RATE_ADJ");
            entity.Property(e => e.FlsaCategory)
                .HasMaxLength(1)
                .HasColumnName("FLSA_CATEGORY");
            entity.Property(e => e.GlExpense)
                .HasMaxLength(35)
                .HasColumnName("GL_EXPENSE");
            entity.Property(e => e.HrlyRtMaximum)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("HRLY_RT_MAXIMUM");
            entity.Property(e => e.HrsDistSw)
                .HasMaxLength(1)
                .HasColumnName("HRS_DIST_SW");
            entity.Property(e => e.HrsOnly)
                .HasMaxLength(1)
                .HasColumnName("HRS_ONLY");
            entity.Property(e => e.IncomeCd1042)
                .HasMaxLength(2)
                .HasColumnName("INCOME_CD_1042");
            entity.Property(e => e.MaintainBalances)
                .HasMaxLength(1)
                .HasColumnName("MAINTAIN_BALANCES");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(1)
                .HasColumnName("PAYMENT_TYPE");
            entity.Property(e => e.PerunitOvrRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("PERUNIT_OVR_RT");
            entity.Property(e => e.PnaUseSglEmpl)
                .HasMaxLength(1)
                .HasColumnName("PNA_USE_SGL_EMPL");
            entity.Property(e => e.RegPayIncluded)
                .HasMaxLength(1)
                .HasColumnName("REG_PAY_INCLUDED");
            entity.Property(e => e.ShiftDiffElig)
                .HasMaxLength(1)
                .HasColumnName("SHIFT_DIFF_ELIG");
            entity.Property(e => e.SpecCalcRtn)
                .HasMaxLength(1)
                .HasColumnName("SPEC_CALC_RTN");
            entity.Property(e => e.SubjectFica)
                .HasMaxLength(1)
                .HasColumnName("SUBJECT_FICA");
            entity.Property(e => e.SubjectFut)
                .HasMaxLength(1)
                .HasColumnName("SUBJECT_FUT");
            entity.Property(e => e.SubjectFwt)
                .HasMaxLength(1)
                .HasColumnName("SUBJECT_FWT");
            entity.Property(e => e.SubjectReg)
                .HasMaxLength(1)
                .HasColumnName("SUBJECT_REG");
            entity.Property(e => e.SubtractEarns)
                .HasMaxLength(1)
                .HasColumnName("SUBTRACT_EARNS");
            entity.Property(e => e.TaxGrsCompnt)
                .HasMaxLength(5)
                .HasColumnName("TAX_GRS_COMPNT");
            entity.Property(e => e.TaxMethod)
                .HasMaxLength(1)
                .HasColumnName("TAX_METHOD");
            entity.Property(e => e.TipsCategory)
                .HasMaxLength(1)
                .HasColumnName("TIPS_CATEGORY");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UsedToPayRetro)
                .HasMaxLength(1)
                .HasColumnName("USED_TO_PAY_RETRO");
            entity.Property(e => e.WithholdFwt)
                .HasMaxLength(1)
                .HasColumnName("WITHHOLD_FWT");
        });

        modelBuilder.Entity<PsEmailAddressesV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_EMAIL_ADDRESSES_V");

            entity.HasIndex(e => new { e.Emplid, e.EAddrType }, "ucpathods_ps_email_emplid_type");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EAddrType)
                .HasMaxLength(4)
                .HasColumnName("E_ADDR_TYPE");
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(70)
                .HasColumnName("EMAIL_ADDR");
            entity.Property(e => e.EmailSvmPrimaryEmail)
                .HasMaxLength(2)
                .HasColumnName("EMAIL__SVM_PRIMARY_EMAIL");
            entity.Property(e => e.EmailSvmSeqMrf).HasColumnName("EMAIL__SVM_SEQ_MRF");
            entity.Property(e => e.EmailSvmSeqNum).HasColumnName("EMAIL__SVM_SEQ_NUM");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PrefEmailFlag)
                .HasMaxLength(1)
                .HasColumnName("PREF_EMAIL_FLAG");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsEmplClassTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_EMPL_CLASS_TBL_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DataTypeCd)
                .HasMaxLength(1)
                .HasColumnName("DATA_TYPE_CD");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EmplClass)
                .HasMaxLength(3)
                .HasColumnName("EMPL_CLASS");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsErnProgramTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ERN_PROGRAM_TBL_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.ErnProgram)
                .HasMaxLength(3)
                .HasColumnName("ERN_PROGRAM");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsEthnicGrpTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_ETHNIC_GRP_TBL_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr50)
                .HasMaxLength(50)
                .HasColumnName("DESCR50");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EthnicCategory)
                .HasMaxLength(1)
                .HasColumnName("ETHNIC_CATEGORY");
            entity.Property(e => e.EthnicGroup)
                .HasMaxLength(1)
                .HasColumnName("ETHNIC_GROUP");
            entity.Property(e => e.EthnicGrpCd)
                .HasMaxLength(8)
                .HasColumnName("ETHNIC_GRP_CD");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsGpAbsEaStaV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_GP_ABS_EA_STA_V");

            entity.Property(e => e.AbsEntrySrc)
                .HasMaxLength(1)
                .HasColumnName("ABS_ENTRY_SRC");
            entity.Property(e => e.AbsenceReason)
                .HasMaxLength(3)
                .HasColumnName("ABSENCE_REASON");
            entity.Property(e => e.ActionDt).HasColumnName("ACTION_DT");
            entity.Property(e => e.BgnDt).HasColumnName("BGN_DT");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EligGrp)
                .HasMaxLength(10)
                .HasColumnName("ELIG_GRP");
            entity.Property(e => e.EmplidCurrAppr)
                .HasMaxLength(11)
                .HasColumnName("EMPLID_CURR_APPR");
            entity.Property(e => e.EndDt).HasColumnName("END_DT");
            entity.Property(e => e.HrWfAction)
                .HasMaxLength(3)
                .HasColumnName("HR_WF_ACTION");
            entity.Property(e => e.LastUpdtDt).HasColumnName("LAST_UPDT_DT");
            entity.Property(e => e.ManagerApprInd)
                .HasMaxLength(1)
                .HasColumnName("MANAGER_APPR_IND");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Oprid)
                .HasMaxLength(30)
                .HasColumnName("OPRID");
            entity.Property(e => e.PinTakeNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_TAKE_NUM");
            entity.Property(e => e.ReturnDt).HasColumnName("RETURN_DT");
            entity.Property(e => e.Seqnum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SEQNUM");
            entity.Property(e => e.TransactionNbr)
                .HasColumnType("numeric(15, 0)")
                .HasColumnName("TRANSACTION_NBR");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.WfStatus)
                .HasMaxLength(1)
                .HasColumnName("WF_STATUS");
        });

        modelBuilder.Entity<PsGpAbsEaV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_GP_ABS_EA_V");

            entity.Property(e => e.AbsEntrySrc)
                .HasMaxLength(1)
                .HasColumnName("ABS_ENTRY_SRC");
            entity.Property(e => e.AbsenceReason)
                .HasMaxLength(3)
                .HasColumnName("ABSENCE_REASON");
            entity.Property(e => e.BgnDt).HasColumnName("BGN_DT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EndDt).HasColumnName("END_DT");
            entity.Property(e => e.LastUpdtDt).HasColumnName("LAST_UPDT_DT");
            entity.Property(e => e.ManagerApprInd)
                .HasMaxLength(1)
                .HasColumnName("MANAGER_APPR_IND");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PinTakeNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_TAKE_NUM");
            entity.Property(e => e.RequestDt).HasColumnName("REQUEST_DT");
            entity.Property(e => e.ReturnDt).HasColumnName("RETURN_DT");
            entity.Property(e => e.TransactionNbr)
                .HasColumnType("numeric(15, 0)")
                .HasColumnName("TRANSACTION_NBR");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.WfStatus)
                .HasMaxLength(1)
                .HasColumnName("WF_STATUS");
        });

        modelBuilder.Entity<PsGpAbsEventV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_GP_ABS_EVENT_V");

            entity.Property(e => e.AbsCanReason)
                .HasMaxLength(3)
                .HasColumnName("ABS_CAN_REASON");
            entity.Property(e => e.AbsEntrySrc)
                .HasMaxLength(1)
                .HasColumnName("ABS_ENTRY_SRC");
            entity.Property(e => e.AbsEvtFcstVal)
                .HasMaxLength(30)
                .HasColumnName("ABS_EVT_FCST_VAL");
            entity.Property(e => e.AbsenceReason)
                .HasMaxLength(3)
                .HasColumnName("ABSENCE_REASON");
            entity.Property(e => e.ActionDtSs).HasColumnName("ACTION_DT_SS");
            entity.Property(e => e.AllDaysInd)
                .HasMaxLength(1)
                .HasColumnName("ALL_DAYS_IND");
            entity.Property(e => e.BeginDayHalfInd)
                .HasMaxLength(1)
                .HasColumnName("BEGIN_DAY_HALF_IND");
            entity.Property(e => e.BeginDayHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("BEGIN_DAY_HRS");
            entity.Property(e => e.BgnDt).HasColumnName("BGN_DT");
            entity.Property(e => e.CalRunId)
                .HasMaxLength(18)
                .HasColumnName("CAL_RUN_ID");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.CurrencyCd1)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD1");
            entity.Property(e => e.CurrencyCd2)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD2");
            entity.Property(e => e.CurrencyCd3)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD3");
            entity.Property(e => e.CurrencyCd4)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD4");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.DurationAbs)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("DURATION_ABS");
            entity.Property(e => e.DurationDys)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("DURATION_DYS");
            entity.Property(e => e.DurationHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("DURATION_HOURS");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EndDayHalfInd)
                .HasMaxLength(1)
                .HasColumnName("END_DAY_HALF_IND");
            entity.Property(e => e.EndDayHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("END_DAY_HRS");
            entity.Property(e => e.EndDt).HasColumnName("END_DT");
            entity.Property(e => e.EndTime).HasColumnName("END_TIME");
            entity.Property(e => e.EndTime2).HasColumnName("END_TIME2");
            entity.Property(e => e.EvtConfig1)
                .HasMaxLength(10)
                .HasColumnName("EVT_CONFIG1");
            entity.Property(e => e.EvtConfig1Dec)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG1_DEC");
            entity.Property(e => e.EvtConfig1Dt).HasColumnName("EVT_CONFIG1_DT");
            entity.Property(e => e.EvtConfig1Mon)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG1_MON");
            entity.Property(e => e.EvtConfig2)
                .HasMaxLength(10)
                .HasColumnName("EVT_CONFIG2");
            entity.Property(e => e.EvtConfig2Dec)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG2_DEC");
            entity.Property(e => e.EvtConfig2Dt).HasColumnName("EVT_CONFIG2_DT");
            entity.Property(e => e.EvtConfig2Mon)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG2_MON");
            entity.Property(e => e.EvtConfig3)
                .HasMaxLength(10)
                .HasColumnName("EVT_CONFIG3");
            entity.Property(e => e.EvtConfig3Dec)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG3_DEC");
            entity.Property(e => e.EvtConfig3Dt).HasColumnName("EVT_CONFIG3_DT");
            entity.Property(e => e.EvtConfig3Mon)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG3_MON");
            entity.Property(e => e.EvtConfig4)
                .HasMaxLength(10)
                .HasColumnName("EVT_CONFIG4");
            entity.Property(e => e.EvtConfig4Dec)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG4_DEC");
            entity.Property(e => e.EvtConfig4Dt).HasColumnName("EVT_CONFIG4_DT");
            entity.Property(e => e.EvtConfig4Mon)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("EVT_CONFIG4_MON");
            entity.Property(e => e.FcstDttm).HasColumnName("FCST_DTTM");
            entity.Property(e => e.FirstProcDt).HasColumnName("FIRST_PROC_DT");
            entity.Property(e => e.LastUpdtDt).HasColumnName("LAST_UPDT_DT");
            entity.Property(e => e.ManagerApprInd)
                .HasMaxLength(1)
                .HasColumnName("MANAGER_APPR_IND");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OrigBeginDt).HasColumnName("ORIG_BEGIN_DT");
            entity.Property(e => e.OvrdAdjVal)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("OVRD_ADJ_VAL");
            entity.Property(e => e.OvrdEntVal)
                .HasColumnType("numeric(10, 6)")
                .HasColumnName("OVRD_ENT_VAL");
            entity.Property(e => e.PinTakeNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_TAKE_NUM");
            entity.Property(e => e.PrcEvtActnOptn)
                .HasMaxLength(1)
                .HasColumnName("PRC_EVT_ACTN_OPTN");
            entity.Property(e => e.ProcessDt).HasColumnName("PROCESS_DT");
            entity.Property(e => e.PyeRunNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PYE_RUN_NUM");
            entity.Property(e => e.StartTime).HasColumnName("START_TIME");
            entity.Property(e => e.StartTime2).HasColumnName("START_TIME2");
            entity.Property(e => e.TransactionNbr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("TRANSACTION_NBR");
            entity.Property(e => e.TransactionNbrEa)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("TRANSACTION_NBR_EA");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.VoidedInd)
                .HasMaxLength(1)
                .HasColumnName("VOIDED_IND");
            entity.Property(e => e.WfStatus)
                .HasMaxLength(1)
                .HasColumnName("WF_STATUS");
        });

        modelBuilder.Entity<PsGpAbsReasonV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_GP_ABS_REASON_V");

            entity.Property(e => e.AbsTypeOptn)
                .HasMaxLength(3)
                .HasColumnName("ABS_TYPE_OPTN");
            entity.Property(e => e.AbsenceReason)
                .HasMaxLength(3)
                .HasColumnName("ABSENCE_REASON");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UsedBy)
                .HasMaxLength(1)
                .HasColumnName("USED_BY");
        });

        modelBuilder.Entity<PsGpPinV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_GP_PIN_V");

            entity.Property(e => e.AutoAssignedType)
                .HasMaxLength(2)
                .HasColumnName("AUTO_ASSIGNED_TYPE");
            entity.Property(e => e.CheckGenerInd)
                .HasMaxLength(1)
                .HasColumnName("CHECK_GENER_IND");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DefnAsofdtOptn)
                .HasMaxLength(1)
                .HasColumnName("DEFN_ASOFDT_OPTN");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EntryTypeUserF1)
                .HasMaxLength(3)
                .HasColumnName("ENTRY_TYPE_USER_F1");
            entity.Property(e => e.EntryTypeUserF2)
                .HasMaxLength(3)
                .HasColumnName("ENTRY_TYPE_USER_F2");
            entity.Property(e => e.EntryTypeUserF3)
                .HasMaxLength(3)
                .HasColumnName("ENTRY_TYPE_USER_F3");
            entity.Property(e => e.EntryTypeUserF4)
                .HasMaxLength(3)
                .HasColumnName("ENTRY_TYPE_USER_F4");
            entity.Property(e => e.EntryTypeUserF5)
                .HasMaxLength(3)
                .HasColumnName("ENTRY_TYPE_USER_F5");
            entity.Property(e => e.EntryTypeUserF6)
                .HasMaxLength(3)
                .HasColumnName("ENTRY_TYPE_USER_F6");
            entity.Property(e => e.FcstInd)
                .HasMaxLength(1)
                .HasColumnName("FCST_IND");
            entity.Property(e => e.FcstReqInd)
                .HasMaxLength(1)
                .HasColumnName("FCST_REQ_IND");
            entity.Property(e => e.FldFmt)
                .HasMaxLength(1)
                .HasColumnName("FLD_FMT");
            entity.Property(e => e.GpVersion)
                .HasMaxLength(18)
                .HasColumnName("GP_VERSION");
            entity.Property(e => e.LastUpdtDttm).HasColumnName("LAST_UPDT_DTTM");
            entity.Property(e => e.LastUpdtOprid)
                .HasMaxLength(30)
                .HasColumnName("LAST_UPDT_OPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OvrdIndCal)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_CAL");
            entity.Property(e => e.OvrdIndElem)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_ELEM");
            entity.Property(e => e.OvrdIndPg)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_PG");
            entity.Property(e => e.OvrdIndPi)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_PI");
            entity.Property(e => e.OvrdIndPye)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_PYE");
            entity.Property(e => e.OvrdIndPyent)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_PYENT");
            entity.Property(e => e.OvrdIndSovr)
                .HasMaxLength(1)
                .HasColumnName("OVRD_IND_SOVR");
            entity.Property(e => e.PinCategory)
                .HasMaxLength(4)
                .HasColumnName("PIN_CATEGORY");
            entity.Property(e => e.PinClass)
                .HasMaxLength(2)
                .HasColumnName("PIN_CLASS");
            entity.Property(e => e.PinCode)
                .HasMaxLength(22)
                .HasColumnName("PIN_CODE");
            entity.Property(e => e.PinCustom1)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM1");
            entity.Property(e => e.PinCustom2)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM2");
            entity.Property(e => e.PinCustom3)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM3");
            entity.Property(e => e.PinCustom4)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM4");
            entity.Property(e => e.PinCustom5)
                .HasMaxLength(20)
                .HasColumnName("PIN_CUSTOM5");
            entity.Property(e => e.PinDriverNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_DRIVER_NUM");
            entity.Property(e => e.PinIndustry)
                .HasMaxLength(4)
                .HasColumnName("PIN_INDUSTRY");
            entity.Property(e => e.PinNm)
                .HasMaxLength(18)
                .HasColumnName("PIN_NM");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.PinOwner)
                .HasMaxLength(1)
                .HasColumnName("PIN_OWNER");
            entity.Property(e => e.PinParentNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_PARENT_NUM");
            entity.Property(e => e.PinType)
                .HasMaxLength(2)
                .HasColumnName("PIN_TYPE");
            entity.Property(e => e.PinUserFld1Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_USER_FLD1_NUM");
            entity.Property(e => e.PinUserFld2Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_USER_FLD2_NUM");
            entity.Property(e => e.PinUserFld3Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_USER_FLD3_NUM");
            entity.Property(e => e.PinUserFld4Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_USER_FLD4_NUM");
            entity.Property(e => e.PinUserFld5Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_USER_FLD5_NUM");
            entity.Property(e => e.PinUserFld6Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_USER_FLD6_NUM");
            entity.Property(e => e.RecalcInd)
                .HasMaxLength(1)
                .HasColumnName("RECALC_IND");
            entity.Property(e => e.RtoDeltaUfLvl)
                .HasMaxLength(1)
                .HasColumnName("RTO_DELTA_UF_LVL");
            entity.Property(e => e.StoreRslt)
                .HasMaxLength(1)
                .HasColumnName("STORE_RSLT");
            entity.Property(e => e.StoreRsltIfZero)
                .HasMaxLength(1)
                .HasColumnName("STORE_RSLT_IF_ZERO");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UsedBy)
                .HasMaxLength(1)
                .HasColumnName("USED_BY");
        });

        modelBuilder.Entity<PsGpRsltAcumV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_GP_RSLT_ACUM_V");

            entity.Property(e => e.AcmFromDt).HasColumnName("ACM_FROM_DT");
            entity.Property(e => e.AcmPrdOptn)
                .HasMaxLength(1)
                .HasColumnName("ACM_PRD_OPTN");
            entity.Property(e => e.AcmThruDt).HasColumnName("ACM_THRU_DT");
            entity.Property(e => e.AcmType)
                .HasMaxLength(1)
                .HasColumnName("ACM_TYPE");
            entity.Property(e => e.CalId)
                .HasMaxLength(18)
                .HasColumnName("CAL_ID");
            entity.Property(e => e.CalRunId)
                .HasMaxLength(18)
                .HasColumnName("CAL_RUN_ID");
            entity.Property(e => e.CalcRsltVal)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CALC_RSLT_VAL");
            entity.Property(e => e.CalcVal)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CALC_VAL");
            entity.Property(e => e.CalledInSegInd)
                .HasMaxLength(1)
                .HasColumnName("CALLED_IN_SEG_IND");
            entity.Property(e => e.CorrRtoInd)
                .HasMaxLength(1)
                .HasColumnName("CORR_RTO_IND");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.EmplRcdAcum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD_ACUM");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.GpPaygroup)
                .HasMaxLength(10)
                .HasColumnName("GP_PAYGROUP");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OrigCalRunId)
                .HasMaxLength(18)
                .HasColumnName("ORIG_CAL_RUN_ID");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.PinParentNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_PARENT_NUM");
            entity.Property(e => e.RsltSegNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("RSLT_SEG_NUM");
            entity.Property(e => e.SeqNum8)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SEQ_NUM8");
            entity.Property(e => e.SliceBgnDt).HasColumnName("SLICE_BGN_DT");
            entity.Property(e => e.SliceEndDt).HasColumnName("SLICE_END_DT");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UserAdjVal)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("USER_ADJ_VAL");
            entity.Property(e => e.UserKey1)
                .HasMaxLength(25)
                .HasColumnName("USER_KEY1");
            entity.Property(e => e.UserKey2)
                .HasMaxLength(25)
                .HasColumnName("USER_KEY2");
            entity.Property(e => e.UserKey3)
                .HasMaxLength(25)
                .HasColumnName("USER_KEY3");
            entity.Property(e => e.UserKey4)
                .HasMaxLength(25)
                .HasColumnName("USER_KEY4");
            entity.Property(e => e.UserKey5)
                .HasMaxLength(25)
                .HasColumnName("USER_KEY5");
            entity.Property(e => e.UserKey6)
                .HasMaxLength(25)
                .HasColumnName("USER_KEY6");
            entity.Property(e => e.ValidInSegInd)
                .HasMaxLength(1)
                .HasColumnName("VALID_IN_SEG_IND");
        });

        modelBuilder.Entity<PsJobV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_JOB_V");

            entity.HasIndex(e => e.DmlInd, "ucpathods_ps_job_dml_for_vw_personJobPosition");

            entity.HasIndex(e => e.EmplStatus, "ucpathods_ps_job_empl_status_for_vw_personJobPosition");

            entity.HasIndex(e => e.Emplid, "ucpathods_ps_job_emplid_for_vw_personJobPosition");

            entity.HasIndex(e => new { e.Emplid, e.EmplRcd, e.Effdt, e.Effseq }, "ucpathods_ps_job_v_key");

            entity.HasIndex(e => new { e.JobSvmIsMostrecent, e.JobSvmPrimaryIdx }, "ucpathods_ps_job_v_svm_flags");

            entity.HasIndex(e => e.Deptid, "ucpathods_ps_ps_job_v_deptid");

            entity.HasIndex(e => e.Jobcode, "ucpathods_ps_ps_job_v_jobcode");

            entity.Property(e => e.AbsenceSystemCd)
                .HasMaxLength(3)
                .HasColumnName("ABSENCE_SYSTEM_CD");
            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDt).HasColumnName("ACTION_DT");
            entity.Property(e => e.ActionReason)
                .HasMaxLength(3)
                .HasColumnName("ACTION_REASON");
            entity.Property(e => e.AnnlBenefBaseRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ANNL_BENEF_BASE_RT");
            entity.Property(e => e.AnnualRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ANNUAL_RT");
            entity.Property(e => e.AsgnEndDt).HasColumnName("ASGN_END_DT");
            entity.Property(e => e.AsgnStartDt).HasColumnName("ASGN_START_DT");
            entity.Property(e => e.AutoEndFlg)
                .HasMaxLength(1)
                .HasColumnName("AUTO_END_FLG");
            entity.Property(e => e.BargUnit)
                .HasMaxLength(4)
                .HasColumnName("BARG_UNIT");
            entity.Property(e => e.BasAction)
                .HasMaxLength(3)
                .HasColumnName("BAS_ACTION");
            entity.Property(e => e.BasGroupId)
                .HasMaxLength(3)
                .HasColumnName("BAS_GROUP_ID");
            entity.Property(e => e.BenStatus)
                .HasMaxLength(4)
                .HasColumnName("BEN_STATUS");
            entity.Property(e => e.BenefitSystem)
                .HasMaxLength(2)
                .HasColumnName("BENEFIT_SYSTEM");
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.ChangeAmt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("CHANGE_AMT");
            entity.Property(e => e.ChangePct)
                .HasColumnType("numeric(6, 3)")
                .HasColumnName("CHANGE_PCT");
            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.CobraAction)
                .HasMaxLength(3)
                .HasColumnName("COBRA_ACTION");
            entity.Property(e => e.CompFrequency)
                .HasMaxLength(5)
                .HasColumnName("COMP_FREQUENCY");
            entity.Property(e => e.Company)
                .HasMaxLength(3)
                .HasColumnName("COMPANY");
            entity.Property(e => e.Comprate)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("COMPRATE");
            entity.Property(e => e.ContractNum)
                .HasMaxLength(25)
                .HasColumnName("CONTRACT_NUM");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.CurRtType)
                .HasMaxLength(5)
                .HasColumnName("CUR_RT_TYPE");
            entity.Property(e => e.CurrencyCd)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD");
            entity.Property(e => e.DailyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("DAILY_RT");
            entity.Property(e => e.DeptEntryDt).HasColumnName("DEPT_ENTRY_DT");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DirectlyTipped)
                .HasMaxLength(1)
                .HasColumnName("DIRECTLY_TIPPED");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EarnsDistType)
                .HasMaxLength(1)
                .HasColumnName("EARNS_DIST_TYPE");
            entity.Property(e => e.EeoClass)
                .HasMaxLength(1)
                .HasColumnName("EEO_CLASS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFFSEQ");
            entity.Property(e => e.EligConfig1)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG1");
            entity.Property(e => e.EligConfig2)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG2");
            entity.Property(e => e.EligConfig3)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG3");
            entity.Property(e => e.EligConfig4)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG4");
            entity.Property(e => e.EligConfig5)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG5");
            entity.Property(e => e.EligConfig6)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG6");
            entity.Property(e => e.EligConfig7)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG7");
            entity.Property(e => e.EligConfig8)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG8");
            entity.Property(e => e.EligConfig9)
                .HasMaxLength(10)
                .HasColumnName("ELIG_CONFIG9");
            entity.Property(e => e.EmplClass)
                .HasMaxLength(3)
                .HasColumnName("EMPL_CLASS");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.EmplStatus)
                .HasMaxLength(1)
                .HasColumnName("EMPL_STATUS");
            entity.Property(e => e.EmplType)
                .HasMaxLength(1)
                .HasColumnName("EMPL_TYPE");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EncumbOverride)
                .HasMaxLength(1)
                .HasColumnName("ENCUMB_OVERRIDE");
            entity.Property(e => e.Estabid)
                .HasMaxLength(12)
                .HasColumnName("ESTABID");
            entity.Property(e => e.ExpectedEndDate).HasColumnName("EXPECTED_END_DATE");
            entity.Property(e => e.ExpectedReturnDt).HasColumnName("EXPECTED_RETURN_DT");
            entity.Property(e => e.FicaStatusEe)
                .HasMaxLength(1)
                .HasColumnName("FICA_STATUS_EE");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.Fte)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("FTE");
            entity.Property(e => e.FullPartTime)
                .HasMaxLength(1)
                .HasColumnName("FULL_PART_TIME");
            entity.Property(e => e.GpAsofDtExgRt)
                .HasMaxLength(1)
                .HasColumnName("GP_ASOF_DT_EXG_RT");
            entity.Property(e => e.GpDfltCurrttyp)
                .HasMaxLength(1)
                .HasColumnName("GP_DFLT_CURRTTYP");
            entity.Property(e => e.GpDfltEligGrp)
                .HasMaxLength(1)
                .HasColumnName("GP_DFLT_ELIG_GRP");
            entity.Property(e => e.GpDfltExrtdt)
                .HasMaxLength(1)
                .HasColumnName("GP_DFLT_EXRTDT");
            entity.Property(e => e.GpEligGrp)
                .HasMaxLength(10)
                .HasColumnName("GP_ELIG_GRP");
            entity.Property(e => e.GpPaygroup)
                .HasMaxLength(10)
                .HasColumnName("GP_PAYGROUP");
            entity.Property(e => e.Grade)
                .HasMaxLength(3)
                .HasColumnName("GRADE");
            entity.Property(e => e.GradeEntryDt).HasColumnName("GRADE_ENTRY_DT");
            entity.Property(e => e.HireDt).HasColumnName("HIRE_DT");
            entity.Property(e => e.HolidaySchedule)
                .HasMaxLength(6)
                .HasColumnName("HOLIDAY_SCHEDULE");
            entity.Property(e => e.HourlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("HOURLY_RT");
            entity.Property(e => e.HrStatus)
                .HasMaxLength(1)
                .HasColumnName("HR_STATUS");
            entity.Property(e => e.JobDataSrcCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_DATA_SRC_CD");
            entity.Property(e => e.JobEntryDt).HasColumnName("JOB_ENTRY_DT");
            entity.Property(e => e.JobIndicator)
                .HasMaxLength(1)
                .HasColumnName("JOB_INDICATOR");
            entity.Property(e => e.JobSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JOB__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JobSvmManualSep)
                .HasMaxLength(2)
                .HasColumnName("JOB__SVM_MANUAL_SEP");
            entity.Property(e => e.JobSvmPrimaryIdx)
                .HasMaxLength(2)
                .HasColumnName("JOB__SVM_PRIMARY_IDX");
            entity.Property(e => e.JobSvmSeqMrf).HasColumnName("JOB__SVM_SEQ_MRF");
            entity.Property(e => e.JobSvmSeqNum).HasColumnName("JOB__SVM_SEQ_NUM");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.LastDateWorked).HasColumnName("LAST_DATE_WORKED");
            entity.Property(e => e.LastHireDt).HasColumnName("LAST_HIRE_DT");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.LdwOvr)
                .HasMaxLength(1)
                .HasColumnName("LDW_OVR");
            entity.Property(e => e.Location)
                .HasMaxLength(10)
                .HasColumnName("LOCATION");
            entity.Property(e => e.LstAsgnStartDt).HasColumnName("LST_ASGN_START_DT");
            entity.Property(e => e.LumpSumPay)
                .HasMaxLength(1)
                .HasColumnName("LUMP_SUM_PAY");
            entity.Property(e => e.MonthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("MONTHLY_RT");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OfficerCd)
                .HasMaxLength(1)
                .HasColumnName("OFFICER_CD");
            entity.Property(e => e.PaySystemFlg)
                .HasMaxLength(2)
                .HasColumnName("PAY_SYSTEM_FLG");
            entity.Property(e => e.Paygroup)
                .HasMaxLength(3)
                .HasColumnName("PAYGROUP");
            entity.Property(e => e.PerOrg)
                .HasMaxLength(3)
                .HasColumnName("PER_ORG");
            entity.Property(e => e.PoiType)
                .HasMaxLength(5)
                .HasColumnName("POI_TYPE");
            entity.Property(e => e.PositionEntryDt).HasColumnName("POSITION_ENTRY_DT");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PositionOverride)
                .HasMaxLength(1)
                .HasColumnName("POSITION_OVERRIDE");
            entity.Property(e => e.PosnChangeRecord)
                .HasMaxLength(1)
                .HasColumnName("POSN_CHANGE_RECORD");
            entity.Property(e => e.ProrateCntAmt)
                .HasMaxLength(1)
                .HasColumnName("PRORATE_CNT_AMT");
            entity.Property(e => e.RegRegion)
                .HasMaxLength(5)
                .HasColumnName("REG_REGION");
            entity.Property(e => e.RegTemp)
                .HasMaxLength(1)
                .HasColumnName("REG_TEMP");
            entity.Property(e => e.ReportsTo)
                .HasMaxLength(8)
                .HasColumnName("REPORTS_TO");
            entity.Property(e => e.SalAdminPlan)
                .HasMaxLength(4)
                .HasColumnName("SAL_ADMIN_PLAN");
            entity.Property(e => e.SetidDept)
                .HasMaxLength(5)
                .HasColumnName("SETID_DEPT");
            entity.Property(e => e.SetidEmplClass)
                .HasMaxLength(5)
                .HasColumnName("SETID_EMPL_CLASS");
            entity.Property(e => e.SetidJobcode)
                .HasMaxLength(5)
                .HasColumnName("SETID_JOBCODE");
            entity.Property(e => e.SetidLocation)
                .HasMaxLength(5)
                .HasColumnName("SETID_LOCATION");
            entity.Property(e => e.SetidSalary)
                .HasMaxLength(5)
                .HasColumnName("SETID_SALARY");
            entity.Property(e => e.SetidSupvLvl)
                .HasMaxLength(5)
                .HasColumnName("SETID_SUPV_LVL");
            entity.Property(e => e.Shift)
                .HasMaxLength(1)
                .HasColumnName("SHIFT");
            entity.Property(e => e.ShiftFactor)
                .HasColumnType("numeric(4, 3)")
                .HasColumnName("SHIFT_FACTOR");
            entity.Property(e => e.ShiftRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("SHIFT_RT");
            entity.Property(e => e.StdHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("STD_HOURS");
            entity.Property(e => e.StdHrsFrequency)
                .HasMaxLength(5)
                .HasColumnName("STD_HRS_FREQUENCY");
            entity.Property(e => e.Step)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("STEP");
            entity.Property(e => e.StepEntryDt).HasColumnName("STEP_ENTRY_DT");
            entity.Property(e => e.SupervisorId)
                .HasMaxLength(11)
                .HasColumnName("SUPERVISOR_ID");
            entity.Property(e => e.SupvLvlId)
                .HasMaxLength(8)
                .HasColumnName("SUPV_LVL_ID");
            entity.Property(e => e.TaxLocationCd)
                .HasMaxLength(10)
                .HasColumnName("TAX_LOCATION_CD");
            entity.Property(e => e.TerminationDt).HasColumnName("TERMINATION_DT");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
            entity.Property(e => e.UnionSeniorityDt).HasColumnName("UNION_SENIORITY_DT");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.WorkDayHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("WORK_DAY_HOURS");
        });

        modelBuilder.Entity<PsJobcodeTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_JOBCODE_TBL_V");

            entity.HasIndex(e => e.Jobcode, "ucpathods_ps_jobcode_tbl_for_vw_jobCodeAndGroup");

            entity.Property(e => e.AvailTelework)
                .HasMaxLength(1)
                .HasColumnName("AVAIL_TELEWORK");
            entity.Property(e => e.BargUnit)
                .HasMaxLength(4)
                .HasColumnName("BARG_UNIT");
            entity.Property(e => e.CanNocCd)
                .HasMaxLength(10)
                .HasColumnName("CAN_NOC_CD");
            entity.Property(e => e.CompFrequency)
                .HasMaxLength(5)
                .HasColumnName("COMP_FREQUENCY");
            entity.Property(e => e.Company)
                .HasMaxLength(3)
                .HasColumnName("COMPANY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.CurrencyCd)
                .HasMaxLength(3)
                .HasColumnName("CURRENCY_CD");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DirectlyTipped)
                .HasMaxLength(1)
                .HasColumnName("DIRECTLY_TIPPED");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Eeo1code)
                .HasMaxLength(1)
                .HasColumnName("EEO1CODE");
            entity.Property(e => e.Eeo4code)
                .HasMaxLength(1)
                .HasColumnName("EEO4CODE");
            entity.Property(e => e.Eeo5code)
                .HasMaxLength(2)
                .HasColumnName("EEO5CODE");
            entity.Property(e => e.Eeo6code)
                .HasMaxLength(1)
                .HasColumnName("EEO6CODE");
            entity.Property(e => e.EeoJobGroup)
                .HasMaxLength(4)
                .HasColumnName("EEO_JOB_GROUP");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EgAcademicRank)
                .HasMaxLength(3)
                .HasColumnName("EG_ACADEMIC_RANK");
            entity.Property(e => e.EgGroup)
                .HasMaxLength(6)
                .HasColumnName("EG_GROUP");
            entity.Property(e => e.EncumbSalAmt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ENCUMB_SAL_AMT");
            entity.Property(e => e.EncumbSalOptn)
                .HasMaxLength(3)
                .HasColumnName("ENCUMB_SAL_OPTN");
            entity.Property(e => e.EncumberIndc)
                .HasMaxLength(1)
                .HasColumnName("ENCUMBER_INDC");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.FunctionCd)
                .HasMaxLength(2)
                .HasColumnName("FUNCTION_CD");
            entity.Property(e => e.Grade)
                .HasMaxLength(3)
                .HasColumnName("GRADE");
            entity.Property(e => e.Ipedsscode)
                .HasMaxLength(1)
                .HasColumnName("IPEDSSCODE");
            entity.Property(e => e.JobFamily)
                .HasMaxLength(6)
                .HasColumnName("JOB_FAMILY");
            entity.Property(e => e.JobFunction)
                .HasMaxLength(3)
                .HasColumnName("JOB_FUNCTION");
            entity.Property(e => e.JobSubFunc)
                .HasMaxLength(3)
                .HasColumnName("JOB_SUB_FUNC");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.JobcodeSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JOBCODE__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JobcodeSvmSeqMrf).HasColumnName("JOBCODE__SVM_SEQ_MRF");
            entity.Property(e => e.JobcodeSvmSeqNum).HasColumnName("JOBCODE__SVM_SEQ_NUM");
            entity.Property(e => e.KeyJobcode)
                .HasMaxLength(1)
                .HasColumnName("KEY_JOBCODE");
            entity.Property(e => e.LastUpdateDate).HasColumnName("LAST_UPDATE_DATE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.ManagerLevel)
                .HasMaxLength(2)
                .HasColumnName("MANAGER_LEVEL");
            entity.Property(e => e.MedChkupReq)
                .HasMaxLength(1)
                .HasColumnName("MED_CHKUP_REQ");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PosnMgmtIndc)
                .HasMaxLength(1)
                .HasColumnName("POSN_MGMT_INDC");
            entity.Property(e => e.RegRegion)
                .HasMaxLength(5)
                .HasColumnName("REG_REGION");
            entity.Property(e => e.RegTemp)
                .HasMaxLength(1)
                .HasColumnName("REG_TEMP");
            entity.Property(e => e.RetroPercent)
                .HasColumnType("numeric(6, 4)")
                .HasColumnName("RETRO_PERCENT");
            entity.Property(e => e.RetroRate)
                .HasColumnType("numeric(6, 4)")
                .HasColumnName("RETRO_RATE");
            entity.Property(e => e.SalAdminPlan)
                .HasMaxLength(4)
                .HasColumnName("SAL_ADMIN_PLAN");
            entity.Property(e => e.SalRangeCurrency)
                .HasMaxLength(3)
                .HasColumnName("SAL_RANGE_CURRENCY");
            entity.Property(e => e.SalRangeFreq)
                .HasMaxLength(5)
                .HasColumnName("SAL_RANGE_FREQ");
            entity.Property(e => e.SalRangeMaxRate)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("SAL_RANGE_MAX_RATE");
            entity.Property(e => e.SalRangeMidRate)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("SAL_RANGE_MID_RATE");
            entity.Property(e => e.SalRangeMinRate)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("SAL_RANGE_MIN_RATE");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.SetidSalary)
                .HasMaxLength(5)
                .HasColumnName("SETID_SALARY");
            entity.Property(e => e.StdHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("STD_HOURS");
            entity.Property(e => e.StdHrsFrequency)
                .HasMaxLength(5)
                .HasColumnName("STD_HRS_FREQUENCY");
            entity.Property(e => e.Step)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("STEP");
            entity.Property(e => e.SurveyJobCode)
                .HasMaxLength(8)
                .HasColumnName("SURVEY_JOB_CODE");
            entity.Property(e => e.SurveySalary)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SURVEY_SALARY");
            entity.Property(e => e.TrnProgram)
                .HasMaxLength(6)
                .HasColumnName("TRN_PROGRAM");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UsOccCd)
                .HasMaxLength(4)
                .HasColumnName("US_OCC_CD");
            entity.Property(e => e.UsSocCd)
                .HasMaxLength(10)
                .HasColumnName("US_SOC_CD");
            entity.Property(e => e.WorkersCompCd)
                .HasMaxLength(4)
                .HasColumnName("WORKERS_COMP_CD");
        });

        modelBuilder.Entity<PsJpmCatItemsV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_JPM_CAT_ITEMS_V");

            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.CmCategory)
                .HasMaxLength(1)
                .HasColumnName("CM_CATEGORY");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EducationLvl)
                .HasMaxLength(2)
                .HasColumnName("EDUCATION_LVL");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EpSubLevel)
                .HasMaxLength(1)
                .HasColumnName("EP_SUB_LEVEL");
            entity.Property(e => e.FpDegreeLvl)
                .HasMaxLength(1)
                .HasColumnName("FP_DEGREE_LVL");
            entity.Property(e => e.HpStatsDegLvl)
                .HasMaxLength(2)
                .HasColumnName("HP_STATS_DEG_LVL");
            entity.Property(e => e.JpmCatItemId)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_ITEM_ID");
            entity.Property(e => e.JpmCatItemSrc)
                .HasMaxLength(4)
                .HasColumnName("JPM_CAT_ITEM_SRC");
            entity.Property(e => e.JpmCatType)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_TYPE");
            entity.Property(e => e.JpmDate1).HasColumnName("JPM_DATE_1");
            entity.Property(e => e.JpmDate2).HasColumnName("JPM_DATE_2");
            entity.Property(e => e.JpmDescr90)
                .HasMaxLength(90)
                .HasColumnName("JPM_DESCR90");
            entity.Property(e => e.JpmDuration1)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_DURATION_1");
            entity.Property(e => e.JpmDuration2)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_DURATION_2");
            entity.Property(e => e.JpmDurationType1)
                .HasMaxLength(1)
                .HasColumnName("JPM_DURATION_TYPE1");
            entity.Property(e => e.JpmDurationType2)
                .HasMaxLength(1)
                .HasColumnName("JPM_DURATION_TYPE2");
            entity.Property(e => e.JpmEvalMthd)
                .HasMaxLength(4)
                .HasColumnName("JPM_EVAL_MTHD");
            entity.Property(e => e.JpmText13251)
                .HasMaxLength(1325)
                .HasColumnName("JPM_TEXT1325_1");
            entity.Property(e => e.JpmText13252)
                .HasMaxLength(1325)
                .HasColumnName("JPM_TEXT1325_2");
            entity.Property(e => e.JpmText2541)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_1");
            entity.Property(e => e.JpmText2542)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_2");
            entity.Property(e => e.JpmText2543)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_3");
            entity.Property(e => e.JpmText2544)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_4");
            entity.Property(e => e.JpmYn1)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_1");
            entity.Property(e => e.JpmYn2)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_2");
            entity.Property(e => e.JpmYn3)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_3");
            entity.Property(e => e.JpmYn4)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_4");
            entity.Property(e => e.JpmYn5)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_5");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.NvqLevel)
                .HasMaxLength(1)
                .HasColumnName("NVQ_LEVEL");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.RatingModel)
                .HasMaxLength(4)
                .HasColumnName("RATING_MODEL");
            entity.Property(e => e.SatisfactionMthd)
                .HasMaxLength(1)
                .HasColumnName("SATISFACTION_MTHD");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsJpmCatTypesV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_JPM_CAT_TYPES_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.JpmAdhocFlg)
                .HasMaxLength(1)
                .HasColumnName("JPM_ADHOC_FLG");
            entity.Property(e => e.JpmCatType)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_TYPE");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.SystemDataFlg)
                .HasMaxLength(1)
                .HasColumnName("SYSTEM_DATA_FLG");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsJpmJpItemsV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_JPM_JP_ITEMS_V");

            entity.Property(e => e.AverageGrade)
                .HasMaxLength(5)
                .HasColumnName("AVERAGE_GRADE");
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EpAppraisalId)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EP_APPRAISAL_ID");
            entity.Property(e => e.EvaluationId)
                .HasMaxLength(2)
                .HasColumnName("EVALUATION_ID");
            entity.Property(e => e.FpDegrRequired)
                .HasMaxLength(1)
                .HasColumnName("FP_DEGR_REQUIRED");
            entity.Property(e => e.FpSkilHir)
                .HasMaxLength(1)
                .HasColumnName("FP_SKIL_HIR");
            entity.Property(e => e.FpSkilPrm)
                .HasMaxLength(1)
                .HasColumnName("FP_SKIL_PRM");
            entity.Property(e => e.FpSkilTen)
                .HasMaxLength(1)
                .HasColumnName("FP_SKIL_TEN");
            entity.Property(e => e.FpSubjectCd)
                .HasMaxLength(3)
                .HasColumnName("FP_SUBJECT_CD");
            entity.Property(e => e.IpeSw)
                .HasMaxLength(1)
                .HasColumnName("IPE_SW");
            entity.Property(e => e.JpmAdhocDescr)
                .HasMaxLength(80)
                .HasColumnName("JPM_ADHOC_DESCR");
            entity.Property(e => e.JpmAreaPref1)
                .HasMaxLength(2)
                .HasColumnName("JPM_AREA_PREF_1");
            entity.Property(e => e.JpmAreaPref2)
                .HasMaxLength(2)
                .HasColumnName("JPM_AREA_PREF_2");
            entity.Property(e => e.JpmAreaPref3)
                .HasMaxLength(2)
                .HasColumnName("JPM_AREA_PREF_3");
            entity.Property(e => e.JpmCatItemId)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_ITEM_ID");
            entity.Property(e => e.JpmCatItemQual)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_ITEM_QUAL");
            entity.Property(e => e.JpmCatItemQual2)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_ITEM_QUAL2");
            entity.Property(e => e.JpmCatType)
                .HasMaxLength(12)
                .HasColumnName("JPM_CAT_TYPE");
            entity.Property(e => e.JpmCntryPref1)
                .HasMaxLength(3)
                .HasColumnName("JPM_CNTRY_PREF_1");
            entity.Property(e => e.JpmCntryPref2)
                .HasMaxLength(3)
                .HasColumnName("JPM_CNTRY_PREF_2");
            entity.Property(e => e.JpmCntryPref3)
                .HasMaxLength(3)
                .HasColumnName("JPM_CNTRY_PREF_3");
            entity.Property(e => e.JpmDate1).HasColumnName("JPM_DATE_1");
            entity.Property(e => e.JpmDate2).HasColumnName("JPM_DATE_2");
            entity.Property(e => e.JpmDate3).HasColumnName("JPM_DATE_3");
            entity.Property(e => e.JpmDate4).HasColumnName("JPM_DATE_4");
            entity.Property(e => e.JpmDate5).HasColumnName("JPM_DATE_5");
            entity.Property(e => e.JpmDate6).HasColumnName("JPM_DATE_6");
            entity.Property(e => e.JpmDecimal1)
                .HasColumnType("numeric(9, 2)")
                .HasColumnName("JPM_DECIMAL_1");
            entity.Property(e => e.JpmDecimal2)
                .HasColumnType("numeric(9, 2)")
                .HasColumnName("JPM_DECIMAL_2");
            entity.Property(e => e.JpmEvalMthd)
                .HasMaxLength(4)
                .HasColumnName("JPM_EVAL_MTHD");
            entity.Property(e => e.JpmImportance)
                .HasMaxLength(1)
                .HasColumnName("JPM_IMPORTANCE");
            entity.Property(e => e.JpmInteger1)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_INTEGER_1");
            entity.Property(e => e.JpmInteger2)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_INTEGER_2");
            entity.Property(e => e.JpmInterestLevel)
                .HasMaxLength(1)
                .HasColumnName("JPM_INTEREST_LEVEL");
            entity.Property(e => e.JpmItemKeyId)
                .HasColumnType("numeric(12, 0)")
                .HasColumnName("JPM_ITEM_KEY_ID");
            entity.Property(e => e.JpmJpItemSrc)
                .HasMaxLength(4)
                .HasColumnName("JPM_JP_ITEM_SRC");
            entity.Property(e => e.JpmJpQualSet)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_QUAL_SET");
            entity.Property(e => e.JpmJpQualSet2)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_QUAL_SET2");
            entity.Property(e => e.JpmLocBunit1)
                .HasMaxLength(5)
                .HasColumnName("JPM_LOC_BUNIT_1");
            entity.Property(e => e.JpmLocBunit2)
                .HasMaxLength(5)
                .HasColumnName("JPM_LOC_BUNIT_2");
            entity.Property(e => e.JpmLocation1)
                .HasMaxLength(10)
                .HasColumnName("JPM_LOCATION_1");
            entity.Property(e => e.JpmLocation2)
                .HasMaxLength(10)
                .HasColumnName("JPM_LOCATION_2");
            entity.Property(e => e.JpmMandatory)
                .HasMaxLength(1)
                .HasColumnName("JPM_MANDATORY");
            entity.Property(e => e.JpmMinorCd)
                .HasMaxLength(10)
                .HasColumnName("JPM_MINOR_CD");
            entity.Property(e => e.JpmObstacle1)
                .HasMaxLength(2)
                .HasColumnName("JPM_OBSTACLE_1");
            entity.Property(e => e.JpmParentKeyId)
                .HasColumnType("numeric(12, 0)")
                .HasColumnName("JPM_PARENT_KEY_ID");
            entity.Property(e => e.JpmPct1)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_PCT_1");
            entity.Property(e => e.JpmPct2)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JPM_PCT_2");
            entity.Property(e => e.JpmPersonId1)
                .HasMaxLength(11)
                .HasColumnName("JPM_PERSON_ID_1");
            entity.Property(e => e.JpmProfileId)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROFILE_ID");
            entity.Property(e => e.JpmPrompt1)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_1");
            entity.Property(e => e.JpmPrompt10)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_10");
            entity.Property(e => e.JpmPrompt11)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_11");
            entity.Property(e => e.JpmPrompt12)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_12");
            entity.Property(e => e.JpmPrompt13)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_13");
            entity.Property(e => e.JpmPrompt14)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_14");
            entity.Property(e => e.JpmPrompt15)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_15");
            entity.Property(e => e.JpmPrompt16)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_16");
            entity.Property(e => e.JpmPrompt17)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_17");
            entity.Property(e => e.JpmPrompt18)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_18");
            entity.Property(e => e.JpmPrompt19)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_19");
            entity.Property(e => e.JpmPrompt2)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_2");
            entity.Property(e => e.JpmPrompt20)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_20");
            entity.Property(e => e.JpmPrompt3)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_3");
            entity.Property(e => e.JpmPrompt4)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_4");
            entity.Property(e => e.JpmPrompt5)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_5");
            entity.Property(e => e.JpmPrompt6)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_6");
            entity.Property(e => e.JpmPrompt7)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_7");
            entity.Property(e => e.JpmPrompt8)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_8");
            entity.Property(e => e.JpmPrompt9)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROMPT_9");
            entity.Property(e => e.JpmRating1)
                .HasMaxLength(1)
                .HasColumnName("JPM_RATING1");
            entity.Property(e => e.JpmRating2)
                .HasMaxLength(1)
                .HasColumnName("JPM_RATING2");
            entity.Property(e => e.JpmRating3)
                .HasMaxLength(1)
                .HasColumnName("JPM_RATING3");
            entity.Property(e => e.JpmSetidLoc1)
                .HasMaxLength(5)
                .HasColumnName("JPM_SETID_LOC_1");
            entity.Property(e => e.JpmSetidLoc2)
                .HasMaxLength(5)
                .HasColumnName("JPM_SETID_LOC_2");
            entity.Property(e => e.JpmSourceId1)
                .HasMaxLength(12)
                .HasColumnName("JPM_SOURCE_ID1");
            entity.Property(e => e.JpmSourceId2)
                .HasMaxLength(12)
                .HasColumnName("JPM_SOURCE_ID2");
            entity.Property(e => e.JpmSourceId3)
                .HasMaxLength(12)
                .HasColumnName("JPM_SOURCE_ID3");
            entity.Property(e => e.JpmText13251)
                .HasMaxLength(1325)
                .HasColumnName("JPM_TEXT1325_1");
            entity.Property(e => e.JpmText13252)
                .HasMaxLength(1325)
                .HasColumnName("JPM_TEXT1325_2");
            entity.Property(e => e.JpmText2541)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_1");
            entity.Property(e => e.JpmText2542)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_2");
            entity.Property(e => e.JpmText2543)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_3");
            entity.Property(e => e.JpmText2544)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_4");
            entity.Property(e => e.JpmText2545)
                .HasMaxLength(254)
                .HasColumnName("JPM_TEXT254_5");
            entity.Property(e => e.JpmVerifyMethod)
                .HasMaxLength(1)
                .HasColumnName("JPM_VERIFY_METHOD");
            entity.Property(e => e.JpmWfStatus)
                .HasMaxLength(2)
                .HasColumnName("JPM_WF_STATUS");
            entity.Property(e => e.JpmYn1)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_1");
            entity.Property(e => e.JpmYn2)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_2");
            entity.Property(e => e.JpmYn3)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_3");
            entity.Property(e => e.JpmYn4)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_4");
            entity.Property(e => e.JpmYn5)
                .HasMaxLength(1)
                .HasColumnName("JPM_YN_5");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.Location)
                .HasMaxLength(10)
                .HasColumnName("LOCATION");
            entity.Property(e => e.MajorCode)
                .HasMaxLength(10)
                .HasColumnName("MAJOR_CODE");
            entity.Property(e => e.MajorDescr)
                .HasMaxLength(100)
                .HasColumnName("MAJOR_DESCR");
            entity.Property(e => e.MinorDescr)
                .HasMaxLength(100)
                .HasColumnName("MINOR_DESCR");
            entity.Property(e => e.NvqStatus)
                .HasMaxLength(1)
                .HasColumnName("NVQ_STATUS");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.RatingModel)
                .HasMaxLength(4)
                .HasColumnName("RATING_MODEL");
            entity.Property(e => e.SchoolCode)
                .HasMaxLength(10)
                .HasColumnName("SCHOOL_CODE");
            entity.Property(e => e.SchoolDescr)
                .HasMaxLength(100)
                .HasColumnName("SCHOOL_DESCR");
            entity.Property(e => e.SchoolType)
                .HasMaxLength(3)
                .HasColumnName("SCHOOL_TYPE");
            entity.Property(e => e.SetidDept)
                .HasMaxLength(5)
                .HasColumnName("SETID_DEPT");
            entity.Property(e => e.SetidLocation)
                .HasMaxLength(5)
                .HasColumnName("SETID_LOCATION");
            entity.Property(e => e.State)
                .HasMaxLength(6)
                .HasColumnName("STATE");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsJpmProfileV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_JPM_PROFILE_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.JpmJpPrflStatus)
                .HasMaxLength(1)
                .HasColumnName("JPM_JP_PRFL_STATUS");
            entity.Property(e => e.JpmJpType)
                .HasMaxLength(12)
                .HasColumnName("JPM_JP_TYPE");
            entity.Property(e => e.JpmLgcyPrflId)
                .HasMaxLength(12)
                .HasColumnName("JPM_LGCY_PRFL_ID");
            entity.Property(e => e.JpmOwnerEmplid)
                .HasMaxLength(11)
                .HasColumnName("JPM_OWNER_EMPLID");
            entity.Property(e => e.JpmProfileId)
                .HasMaxLength(12)
                .HasColumnName("JPM_PROFILE_ID");
            entity.Property(e => e.JpmProfileUsage)
                .HasMaxLength(1)
                .HasColumnName("JPM_PROFILE_USAGE");
            entity.Property(e => e.JpmSubscribe)
                .HasMaxLength(1)
                .HasColumnName("JPM_SUBSCRIBE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.StatusDt).HasColumnName("STATUS_DT");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsPerOrgInstV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_PER_ORG_INST_V");

            entity.HasIndex(e => e.Emplid, "ucpathods_ps_per_org_id_for_vwPerson");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.NeeProviderId)
                .HasMaxLength(10)
                .HasColumnName("NEE_PROVIDER_ID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OrgInstSrvDt).HasColumnName("ORG_INST_SRV_DT");
            entity.Property(e => e.OrgInstSrvOvr)
                .HasMaxLength(1)
                .HasColumnName("ORG_INST_SRV_OVR");
            entity.Property(e => e.OrgInstanceErn)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_INSTANCE_ERN");
            entity.Property(e => e.OrigHireDt).HasColumnName("ORIG_HIRE_DT");
            entity.Property(e => e.OrigHireOvr)
                .HasMaxLength(1)
                .HasColumnName("ORIG_HIRE_OVR");
            entity.Property(e => e.PerOrg)
                .HasMaxLength(3)
                .HasColumnName("PER_ORG");
            entity.Property(e => e.PerorgSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PERORG__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PerorgSvmSeqMrf).HasColumnName("PERORG__SVM_SEQ_MRF");
            entity.Property(e => e.PerorgSvmSeqNum).HasColumnName("PERORG__SVM_SEQ_NUM");
            entity.Property(e => e.PoiType)
                .HasMaxLength(5)
                .HasColumnName("POI_TYPE");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsPersDataEffdtV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_PERS_DATA_EFFDT_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.FtStudent)
                .HasMaxLength(1)
                .HasColumnName("FT_STUDENT");
            entity.Property(e => e.HighestEducLvl)
                .HasMaxLength(2)
                .HasColumnName("HIGHEST_EDUC_LVL");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Pronoun)
                .HasMaxLength(2)
                .HasColumnName("PRONOUN");
            entity.Property(e => e.PsPersDataSvmDeptSeqNum).HasColumnName("PS_PERS_DATA__SVM_DEPT_SEQ_NUM");
            entity.Property(e => e.PsPersDataSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PS_PERS_DATA__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsPersDataSvmPrimary)
                .HasMaxLength(2)
                .HasColumnName("PS_PERS_DATA__SVM_PRIMARY");
            entity.Property(e => e.PsPersDataSvmSeqMrf).HasColumnName("PS_PERS_DATA__SVM_SEQ_MRF");
            entity.Property(e => e.PsPersDataSvmSeqNum).HasColumnName("PS_PERS_DATA__SVM_SEQ_NUM");
            entity.Property(e => e.RegRegion)
                .HasMaxLength(5)
                .HasColumnName("REG_REGION");
            entity.Property(e => e.Sex)
                .HasMaxLength(1)
                .HasColumnName("SEX");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsPersonV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_PERSON_V");

            entity.HasIndex(e => e.Emplid, "ucpathods_ps_person_emplid");

            entity.Property(e => e.Birthcountry)
                .HasMaxLength(3)
                .HasColumnName("BIRTHCOUNTRY");
            entity.Property(e => e.Birthdate).HasColumnName("BIRTHDATE");
            entity.Property(e => e.Birthplace)
                .HasMaxLength(30)
                .HasColumnName("BIRTHPLACE");
            entity.Property(e => e.Birthstate)
                .HasMaxLength(6)
                .HasColumnName("BIRTHSTATE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.DtOfDeath).HasColumnName("DT_OF_DEATH");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.LastChildUpddtm).HasColumnName("LAST_CHILD_UPDDTM");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PersonSvmPrimary)
                .HasMaxLength(2)
                .HasColumnName("PERSON__SVM_PRIMARY");
            entity.Property(e => e.PersonSvmSeqNum).HasColumnName("PERSON__SVM_SEQ_NUM");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsPersonalPhoneV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_PERSONAL_PHONE_V");

            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Extension)
                .HasMaxLength(6)
                .HasColumnName("EXTENSION");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Phone)
                .HasMaxLength(24)
                .HasColumnName("PHONE");
            entity.Property(e => e.PhoneType)
                .HasMaxLength(4)
                .HasColumnName("PHONE_TYPE");
            entity.Property(e => e.PrefPhoneFlag)
                .HasMaxLength(1)
                .HasColumnName("PREF_PHONE_FLAG");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsPositionDataV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_POSITION_DATA_V");

            entity.HasIndex(e => e.Deptid, "ucpathods_ps_position_data_v_deptid");

            entity.HasIndex(e => e.Jobcode, "ucpathods_ps_position_data_v_jobcode");

            entity.HasIndex(e => new { e.PositionNbr, e.Effdt }, "ucpathods_ps_position_data_v_key");

            entity.HasIndex(e => e.PosSvmIsMostrecent, "ucpathods_ps_position_data_v_svm_flags");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDt).HasColumnName("ACTION_DT");
            entity.Property(e => e.ActionReason)
                .HasMaxLength(3)
                .HasColumnName("ACTION_REASON");
            entity.Property(e => e.AddsToFteActual)
                .HasMaxLength(1)
                .HasColumnName("ADDS_TO_FTE_ACTUAL");
            entity.Property(e => e.AvailTeleworkPos)
                .HasMaxLength(1)
                .HasColumnName("AVAIL_TELEWORK_POS");
            entity.Property(e => e.BargUnit)
                .HasMaxLength(4)
                .HasColumnName("BARG_UNIT");
            entity.Property(e => e.BudgetedPosn)
                .HasMaxLength(1)
                .HasColumnName("BUDGETED_POSN");
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.Company)
                .HasMaxLength(3)
                .HasColumnName("COMPANY");
            entity.Property(e => e.ConfidentialPosn)
                .HasMaxLength(1)
                .HasColumnName("CONFIDENTIAL_POSN");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EgAcademicRank)
                .HasMaxLength(3)
                .HasColumnName("EG_ACADEMIC_RANK");
            entity.Property(e => e.EgGroup)
                .HasMaxLength(6)
                .HasColumnName("EG_GROUP");
            entity.Property(e => e.EncumbSalAmt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ENCUMB_SAL_AMT");
            entity.Property(e => e.EncumbSalOptn)
                .HasMaxLength(3)
                .HasColumnName("ENCUMB_SAL_OPTN");
            entity.Property(e => e.EncumberIndc)
                .HasMaxLength(1)
                .HasColumnName("ENCUMBER_INDC");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.FriHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("FRI_HRS");
            entity.Property(e => e.Fte)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("FTE");
            entity.Property(e => e.FullPartTime)
                .HasMaxLength(1)
                .HasColumnName("FULL_PART_TIME");
            entity.Property(e => e.Grade)
                .HasMaxLength(3)
                .HasColumnName("GRADE");
            entity.Property(e => e.GvtCyberSecCd)
                .HasMaxLength(3)
                .HasColumnName("GVT_CYBER_SEC_CD");
            entity.Property(e => e.HealthCertificate)
                .HasMaxLength(1)
                .HasColumnName("HEALTH_CERTIFICATE");
            entity.Property(e => e.IncludeSalplnFlg)
                .HasMaxLength(1)
                .HasColumnName("INCLUDE_SALPLN_FLG");
            entity.Property(e => e.JobShare)
                .HasMaxLength(1)
                .HasColumnName("JOB_SHARE");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.KeyPosition)
                .HasMaxLength(1)
                .HasColumnName("KEY_POSITION");
            entity.Property(e => e.LanguageSkill)
                .HasMaxLength(2)
                .HasColumnName("LANGUAGE_SKILL");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.Location)
                .HasMaxLength(10)
                .HasColumnName("LOCATION");
            entity.Property(e => e.MailDrop)
                .HasMaxLength(50)
                .HasColumnName("MAIL_DROP");
            entity.Property(e => e.ManagerLevel)
                .HasMaxLength(2)
                .HasColumnName("MANAGER_LEVEL");
            entity.Property(e => e.MaxHeadCount)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("MAX_HEAD_COUNT");
            entity.Property(e => e.MonHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("MON_HRS");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Orgcode)
                .HasMaxLength(60)
                .HasColumnName("ORGCODE");
            entity.Property(e => e.OrgcodeFlag)
                .HasMaxLength(1)
                .HasColumnName("ORGCODE_FLAG");
            entity.Property(e => e.Phone)
                .HasMaxLength(24)
                .HasColumnName("PHONE");
            entity.Property(e => e.PosSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("POS__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PosSvmSeqMrf).HasColumnName("POS__SVM_SEQ_MRF");
            entity.Property(e => e.PosSvmSeqNum).HasColumnName("POS__SVM_SEQ_NUM");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PositionPoolId)
                .HasMaxLength(3)
                .HasColumnName("POSITION_POOL_ID");
            entity.Property(e => e.PosnStatus)
                .HasMaxLength(1)
                .HasColumnName("POSN_STATUS");
            entity.Property(e => e.RegRegion)
                .HasMaxLength(5)
                .HasColumnName("REG_REGION");
            entity.Property(e => e.RegTemp)
                .HasMaxLength(1)
                .HasColumnName("REG_TEMP");
            entity.Property(e => e.ReportDottedLine)
                .HasMaxLength(8)
                .HasColumnName("REPORT_DOTTED_LINE");
            entity.Property(e => e.ReportsTo)
                .HasMaxLength(8)
                .HasColumnName("REPORTS_TO");
            entity.Property(e => e.SalAdminPlan)
                .HasMaxLength(4)
                .HasColumnName("SAL_ADMIN_PLAN");
            entity.Property(e => e.SatHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("SAT_HRS");
            entity.Property(e => e.Seasonal)
                .HasMaxLength(1)
                .HasColumnName("SEASONAL");
            entity.Property(e => e.SecClearanceType)
                .HasMaxLength(3)
                .HasColumnName("SEC_CLEARANCE_TYPE");
            entity.Property(e => e.Shift)
                .HasMaxLength(1)
                .HasColumnName("SHIFT");
            entity.Property(e => e.SignAuthority)
                .HasMaxLength(1)
                .HasColumnName("SIGN_AUTHORITY");
            entity.Property(e => e.StatusDt).HasColumnName("STATUS_DT");
            entity.Property(e => e.StdHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("STD_HOURS");
            entity.Property(e => e.StdHrsFrequency)
                .HasMaxLength(5)
                .HasColumnName("STD_HRS_FREQUENCY");
            entity.Property(e => e.Step)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("STEP");
            entity.Property(e => e.SunHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("SUN_HRS");
            entity.Property(e => e.SupvLvlId)
                .HasMaxLength(8)
                .HasColumnName("SUPV_LVL_ID");
            entity.Property(e => e.ThursHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("THURS_HRS");
            entity.Property(e => e.TrnProgram)
                .HasMaxLength(6)
                .HasColumnName("TRN_PROGRAM");
            entity.Property(e => e.TuesHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("TUES_HRS");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.UpdateIncumbents)
                .HasMaxLength(1)
                .HasColumnName("UPDATE_INCUMBENTS");
            entity.Property(e => e.WedHrs)
                .HasColumnType("numeric(4, 2)")
                .HasColumnName("WED_HRS");
        });

        modelBuilder.Entity<PsPrimaryJobsV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_PRIMARY_JOBS_V");

            entity.HasIndex(e => new { e.Emplid, e.EmplRcd, e.Effdt }, "ucpathods_ps_primary_job_v_key");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.JobEffseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_EFFSEQ");
            entity.Property(e => e.JobEmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_EMPL_RCD");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PriJobsSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PRI_JOBS__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PriJobsSvmSeqMrf).HasColumnName("PRI_JOBS__SVM_SEQ_MRF");
            entity.Property(e => e.PriJobsSvmSeqNum).HasColumnName("PRI_JOBS__SVM_SEQ_NUM");
            entity.Property(e => e.PrimaryFlag1)
                .HasMaxLength(1)
                .HasColumnName("PRIMARY_FLAG1");
            entity.Property(e => e.PrimaryFlag2)
                .HasMaxLength(1)
                .HasColumnName("PRIMARY_FLAG2");
            entity.Property(e => e.PrimaryJobApp)
                .HasMaxLength(2)
                .HasColumnName("PRIMARY_JOB_APP");
            entity.Property(e => e.PrimaryJobInd)
                .HasMaxLength(1)
                .HasColumnName("PRIMARY_JOB_IND");
            entity.Property(e => e.PrimaryJobsSrc)
                .HasMaxLength(1)
                .HasColumnName("PRIMARY_JOBS_SRC");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcAmSsRcdV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_AM_SS_RCD_V");

            entity.Property(e => e.Asofdate).HasColumnName("ASOFDATE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DescrDept)
                .HasMaxLength(30)
                .HasColumnName("DESCR_DEPT");
            entity.Property(e => e.DescrJobcode)
                .HasMaxLength(30)
                .HasColumnName("DESCR_JOBCODE");
            entity.Property(e => e.DescrPosition)
                .HasMaxLength(30)
                .HasColumnName("DESCR_POSITION");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EligGrp)
                .HasMaxLength(10)
                .HasColumnName("ELIG_GRP");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PayPeriodHrs)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("PAY_PERIOD_HRS");
            entity.Property(e => e.ServiceInd)
                .HasMaxLength(1)
                .HasColumnName("SERVICE_IND");
            entity.Property(e => e.UcAccrFactorP)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("UC_ACCR_FACTOR_P");
            entity.Property(e => e.UcAccrFactorS)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("UC_ACCR_FACTOR_S");
            entity.Property(e => e.UcAccrFactorV)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("UC_ACCR_FACTOR_V");
            entity.Property(e => e.UcPrdEarnedP)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("UC_PRD_EARNED_P");
            entity.Property(e => e.UcPrdEarnedS)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("UC_PRD_EARNED_S");
            entity.Property(e => e.UcPrdEarnedV)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("UC_PRD_EARNED_V");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcAmSsTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_AM_SS_TBL_V");

            entity.Property(e => e.Asofdate).HasColumnName("ASOFDATE");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.OrderNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORDER_NUM");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.UcAccrLimit)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_ACCR_LIMIT");
            entity.Property(e => e.UcAmSvmPrimary)
                .HasMaxLength(2)
                .HasColumnName("UC_AM__SVM_PRIMARY");
            entity.Property(e => e.UcAmSvmSeqNum).HasColumnName("UC_AM__SVM_SEQ_NUM");
            entity.Property(e => e.UcAprMaxInd)
                .HasMaxLength(1)
                .HasColumnName("UC_APR_MAX_IND");
            entity.Property(e => e.UcCurrBal)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_CURR_BAL");
            entity.Property(e => e.UcPrdAccrual)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_PRD_ACCRUAL");
            entity.Property(e => e.UcPrdAdjusted)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_PRD_ADJUSTED");
            entity.Property(e => e.UcPrdTaken)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_PRD_TAKEN");
            entity.Property(e => e.UcPrevBal)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("UC_PREV_BAL");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcCtoOscV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_CTO_OSC_V");

            entity.HasIndex(e => e.UcCtoOsCd, "ucpathods_ps_uc_cto_osc_key");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.CtoOscSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("CTO_OSC__SVM_IS_MOSTRECENT");
            entity.Property(e => e.CtoOscSvmSeqMrf).HasColumnName("CTO_OSC__SVM_SEQ_MRF");
            entity.Property(e => e.CtoOscSvmSeqNum).HasColumnName("CTO_OSC__SVM_SEQ_NUM");
            entity.Property(e => e.Descr50)
                .HasMaxLength(50)
                .HasColumnName("DESCR50");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UcCtoOsCd)
                .HasMaxLength(3)
                .HasColumnName("UC_CTO_OS_CD");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcExtSystemV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_EXT_SYSTEM_V");

            entity.HasIndex(e => new { e.Emplid, e.UcExtSystem, e.EffStatus }, "ucpathods_ext_system_key");

            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .HasColumnName("BUSINESS_UNIT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.ExtSystemSvmSeq).HasColumnName("EXT_SYSTEM__SVM_SEQ");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UcExtSystem)
                .HasMaxLength(50)
                .HasColumnName("UC_EXT_SYSTEM");
            entity.Property(e => e.UcExtSystemId)
                .HasMaxLength(254)
                .HasColumnName("UC_EXT_SYSTEM_ID");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcFundAttribV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_FUND_ATTRIB_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.FundCode)
                .HasMaxLength(5)
                .HasColumnName("FUND_CODE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.UcAttribute1)
                .HasMaxLength(1)
                .HasColumnName("UC_ATTRIBUTE1");
            entity.Property(e => e.UcAttribute2)
                .HasMaxLength(1)
                .HasColumnName("UC_ATTRIBUTE2");
            entity.Property(e => e.UcAttribute3)
                .HasMaxLength(1)
                .HasColumnName("UC_ATTRIBUTE3");
            entity.Property(e => e.UcAwardType)
                .HasMaxLength(1)
                .HasColumnName("UC_AWARD_TYPE");
            entity.Property(e => e.UcCaptype)
                .HasMaxLength(3)
                .HasColumnName("UC_CAPTYPE");
            entity.Property(e => e.UcEverify)
                .HasMaxLength(1)
                .HasColumnName("UC_EVERIFY");
            entity.Property(e => e.UcFedFlowThru)
                .HasMaxLength(1)
                .HasColumnName("UC_FED_FLOW_THRU");
            entity.Property(e => e.UcFederal)
                .HasMaxLength(1)
                .HasColumnName("UC_FEDERAL");
            entity.Property(e => e.UcFundEndDt).HasColumnName("UC_FUND_END_DT");
            entity.Property(e => e.UcFundTypeEncum)
                .HasMaxLength(1)
                .HasColumnName("UC_FUND_TYPE_ENCUM");
            entity.Property(e => e.UcGeneral)
                .HasMaxLength(1)
                .HasColumnName("UC_GENERAL");
            entity.Property(e => e.UcSponsorName)
                .HasMaxLength(25)
                .HasColumnName("UC_SPONSOR_NAME");
            entity.Property(e => e.UcSponsorTypeOt)
                .HasMaxLength(2)
                .HasColumnName("UC_SPONSOR_TYPE_OT");
            entity.Property(e => e.UcSponsored)
                .HasMaxLength(1)
                .HasColumnName("UC_SPONSORED");
            entity.Property(e => e.UcStateFund)
                .HasMaxLength(1)
                .HasColumnName("UC_STATE_FUND");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcGpAbsEaV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_GP_ABS_EA_V");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionReason)
                .HasMaxLength(3)
                .HasColumnName("ACTION_REASON");
            entity.Property(e => e.BgnDt).HasColumnName("BGN_DT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .HasColumnName("DML_IND");
            entity.Property(e => e.EmplClass)
                .HasMaxLength(3)
                .HasColumnName("EMPL_CLASS");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EndDt).HasColumnName("END_DT");
            entity.Property(e => e.GpAbsFmlaElig)
                .HasMaxLength(1)
                .HasColumnName("GP_ABS_FMLA_ELIG");
            entity.Property(e => e.GpAbsFmlaOvrrd)
                .HasMaxLength(1)
                .HasColumnName("GP_ABS_FMLA_OVRRD");
            entity.Property(e => e.LastDateWorked).HasColumnName("LAST_DATE_WORKED");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PayBeginDt).HasColumnName("PAY_BEGIN_DT");
            entity.Property(e => e.PayEndDt).HasColumnName("PAY_END_DT");
            entity.Property(e => e.PinTakeNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_TAKE_NUM");
            entity.Property(e => e.PinTkNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_TK_NUM");
            entity.Property(e => e.ReturnDt).HasColumnName("RETURN_DT");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.TransactionNbr)
                .HasColumnType("numeric(15, 0)")
                .HasColumnName("TRANSACTION_NBR");
            entity.Property(e => e.UcAbsAddlvCff)
                .HasMaxLength(1)
                .HasColumnName("UC_ABS_ADDLV_CFF");
            entity.Property(e => e.UcAbsAddlvEn1)
                .HasMaxLength(10)
                .HasColumnName("UC_ABS_ADDLV_EN1");
            entity.Property(e => e.UcAbsAddlvEn2)
                .HasMaxLength(10)
                .HasColumnName("UC_ABS_ADDLV_EN2");
            entity.Property(e => e.UcAbsAddlvEn3)
                .HasMaxLength(10)
                .HasColumnName("UC_ABS_ADDLV_EN3");
            entity.Property(e => e.UcAbsAddlvEn4)
                .HasMaxLength(10)
                .HasColumnName("UC_ABS_ADDLV_EN4");
            entity.Property(e => e.UcAbsAddlvEsl)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_ESL");
            entity.Property(e => e.UcAbsAddlvPc1)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_PC1");
            entity.Property(e => e.UcAbsAddlvPc2)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_PC2");
            entity.Property(e => e.UcAbsAddlvPc3)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_PC3");
            entity.Property(e => e.UcAbsAddlvPc4)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_PC4");
            entity.Property(e => e.UcAbsAddlvRgp)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_RGP");
            entity.Property(e => e.UcAbsAddlvSbp)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_SBP");
            entity.Property(e => e.UcAbsAddlvSll)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_SLL");
            entity.Property(e => e.UcAbsAddlvSls)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_SLS");
            entity.Property(e => e.UcAbsAddlvWcn)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_WCN");
            entity.Property(e => e.UcAbsAddlvWcp)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_WCP");
            entity.Property(e => e.UcAbsAddlvWcr)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_WCR");
            entity.Property(e => e.UcAbsAddlvWcs)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_ADDLV_WCS");
            entity.Property(e => e.UcAbsReason)
                .HasMaxLength(3)
                .HasColumnName("UC_ABS_REASON");
            entity.Property(e => e.UcAbsSabCrdt)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_ABS_SAB_CRDT");
            entity.Property(e => e.UcExcludeJob)
                .HasMaxLength(25)
                .HasColumnName("UC_EXCLUDE_JOB");
            entity.Property(e => e.UcFmlaAdjhrs)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("UC_FMLA_ADJHRS");
            entity.Property(e => e.UcGpAbsCfraElg)
                .HasMaxLength(1)
                .HasColumnName("UC_GP_ABS_CFRA_ELG");
            entity.Property(e => e.UcGpAbsCfraOvr)
                .HasMaxLength(1)
                .HasColumnName("UC_GP_ABS_CFRA_OVR");
            entity.Property(e => e.UcGpAbsPfcbOvr)
                .HasMaxLength(1)
                .HasColumnName("UC_GP_ABS_PFCB_OVR");
            entity.Property(e => e.UcPayReturnDt).HasColumnName("UC_PAY_RETURN_DT");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.WfStatus)
                .HasMaxLength(1)
                .HasColumnName("WF_STATUS");
        });

        modelBuilder.Entity<PsUcJobCodeTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_JOB_CODE_TBL_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Estabid)
                .HasMaxLength(12)
                .HasColumnName("ESTABID");
            entity.Property(e => e.JcgSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JCG__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JcgSvmSeqMrf).HasColumnName("JCG__SVM_SEQ_MRF");
            entity.Property(e => e.JcgSvmSeqNum).HasColumnName("JCG__SVM_SEQ_NUM");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UcJobGroup)
                .HasMaxLength(4)
                .HasColumnName("UC_JOB_GROUP");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcJobCodesV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_JOB_CODES_V");

            entity.HasIndex(e => e.Jobcode, "ucpathods_ps_uc_job_codes_for_vw_jobCodeAndGroup");

            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EmplClass)
                .HasMaxLength(3)
                .HasColumnName("EMPL_CLASS");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Setid)
                .HasMaxLength(5)
                .HasColumnName("SETID");
            entity.Property(e => e.UcAcaCompGrpCd)
                .HasMaxLength(3)
                .HasColumnName("UC_ACA_COMP_GRP_CD");
            entity.Property(e => e.UcBnExclElig)
                .HasMaxLength(1)
                .HasColumnName("UC_BN_EXCL_ELIG");
            entity.Property(e => e.UcBnHwElig)
                .HasMaxLength(1)
                .HasColumnName("UC_BN_HW_ELIG");
            entity.Property(e => e.UcByagrmnt)
                .HasMaxLength(1)
                .HasColumnName("UC_BYAGRMNT");
            entity.Property(e => e.UcCtoOsCd)
                .HasMaxLength(3)
                .HasColumnName("UC_CTO_OS_CD");
            entity.Property(e => e.UcEligibleOncall)
                .HasMaxLength(1)
                .HasColumnName("UC_ELIGIBLE_ONCALL");
            entity.Property(e => e.UcEligibleShiftd)
                .HasMaxLength(1)
                .HasColumnName("UC_ELIGIBLE_SHIFTD");
            entity.Property(e => e.UcFacultyIndc)
                .HasMaxLength(1)
                .HasColumnName("UC_FACULTY_INDC");
            entity.Property(e => e.UcOffscale)
                .HasMaxLength(1)
                .HasColumnName("UC_OFFSCALE");
            entity.Property(e => e.UcOshpdCode)
                .HasMaxLength(10)
                .HasColumnName("UC_OSHPD_CODE");
            entity.Property(e => e.UcRetmtSftyCd)
                .HasMaxLength(1)
                .HasColumnName("UC_RETMT_SFTY_CD");
            entity.Property(e => e.UcSummerSal)
                .HasMaxLength(1)
                .HasColumnName("UC_SUMMER_SAL");
            entity.Property(e => e.UcSyswdLcl)
                .HasMaxLength(1)
                .HasColumnName("UC_SYSWD_LCL");
            entity.Property(e => e.UcjobcodeSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("UCJOBCODE__SVM_IS_MOSTRECENT");
            entity.Property(e => e.UcjobcodeSvmSeqMrf).HasColumnName("UCJOBCODE__SVM_SEQ_MRF");
            entity.Property(e => e.UcjobcodeSvmSeqNum).HasColumnName("UCJOBCODE__SVM_SEQ_NUM");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcJobGrpDescV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_JOB_GRP_DESC_V");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Estabid)
                .HasMaxLength(12)
                .HasColumnName("ESTABID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UcJobGroup)
                .HasMaxLength(4)
                .HasColumnName("UC_JOB_GROUP");
            entity.Property(e => e.UcjobgroupSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("UCJOBGROUP__SVM_IS_MOSTRECENT");
            entity.Property(e => e.UcjobgroupSvmSeqMrf).HasColumnName("UCJOBGROUP__SVM_SEQ_MRF");
            entity.Property(e => e.UcjobgroupSvmSeqNum).HasColumnName("UCJOBGROUP__SVM_SEQ_NUM");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcSsDisclosurV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UC_SS_DISCLOSUR_V");

            entity.Property(e => e.ConfirmAccept)
                .HasMaxLength(1)
                .HasColumnName("CONFIRM_ACCEPT");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.UcAddrProc)
                .HasMaxLength(1)
                .HasColumnName("UC_ADDR_PROC");
            entity.Property(e => e.UcAddrRel)
                .HasMaxLength(1)
                .HasColumnName("UC_ADDR_REL");
            entity.Property(e => e.UcDisclosureStat)
                .HasMaxLength(1)
                .HasColumnName("UC_DISCLOSURE_STAT");
            entity.Property(e => e.UcEmailRel)
                .HasMaxLength(1)
                .HasColumnName("UC_EMAIL_REL");
            entity.Property(e => e.UcMobileRel)
                .HasMaxLength(1)
                .HasColumnName("UC_MOBILE_REL");
            entity.Property(e => e.UcPhoneProc)
                .HasMaxLength(1)
                .HasColumnName("UC_PHONE_PROC");
            entity.Property(e => e.UcPhoneRel)
                .HasMaxLength(1)
                .HasColumnName("UC_PHONE_REL");
            entity.Property(e => e.UcSpouseProc)
                .HasMaxLength(1)
                .HasColumnName("UC_SPOUSE_PROC");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
        });

        modelBuilder.Entity<PsUcdDmPsNamesPrefVnamesV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UCD_DM_PS_NAMES_PREF_VNAMES_V");

            entity.HasIndex(e => new { e.LastName, e.FirstName, e.Name }, "ucpathods_ps_ucd_dm_ps_names_pref_vnames_name");

            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.FirstNameSrch)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME_SRCH");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.LastNameSrch)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME_SRCH");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .HasColumnName("MIDDLE_NAME");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.NameDisplay)
                .HasMaxLength(60)
                .HasColumnName("NAME_DISPLAY");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
        });

        modelBuilder.Entity<PsUnionTblV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_UNION_TBL_V");

            entity.HasIndex(e => e.UnionCd, "ucpathods_ps_union_key");

            entity.Property(e => e.Address1)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS1");
            entity.Property(e => e.Address2)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS2");
            entity.Property(e => e.Address3)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS3");
            entity.Property(e => e.Address4)
                .HasMaxLength(55)
                .HasColumnName("ADDRESS4");
            entity.Property(e => e.BargUnit)
                .HasMaxLength(4)
                .HasColumnName("BARG_UNIT");
            entity.Property(e => e.CallbackMinHours)
                .HasColumnType("numeric(2, 1)")
                .HasColumnName("CALLBACK_MIN_HOURS");
            entity.Property(e => e.CallbackRate)
                .HasColumnType("numeric(2, 1)")
                .HasColumnName("CALLBACK_RATE");
            entity.Property(e => e.Certified)
                .HasMaxLength(1)
                .HasColumnName("CERTIFIED");
            entity.Property(e => e.City)
                .HasMaxLength(30)
                .HasColumnName("CITY");
            entity.Property(e => e.ClosedShop)
                .HasMaxLength(1)
                .HasColumnName("CLOSED_SHOP");
            entity.Property(e => e.ContactName)
                .HasMaxLength(50)
                .HasColumnName("CONTACT_NAME");
            entity.Property(e => e.ContractBeginDt).HasColumnName("CONTRACT_BEGIN_DT");
            entity.Property(e => e.ContractEndDt).HasColumnName("CONTRACT_END_DT");
            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.County)
                .HasMaxLength(30)
                .HasColumnName("COUNTY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Descrshort)
                .HasMaxLength(10)
                .HasColumnName("DESCRSHORT");
            entity.Property(e => e.DisabilityIns)
                .HasMaxLength(1)
                .HasColumnName("DISABILITY_INS");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.FicaPickup)
                .HasMaxLength(1)
                .HasColumnName("FICA_PICKUP");
            entity.Property(e => e.GeoCode)
                .HasMaxLength(11)
                .HasColumnName("GEO_CODE");
            entity.Property(e => e.HouseType)
                .HasMaxLength(2)
                .HasColumnName("HOUSE_TYPE");
            entity.Property(e => e.LifeIns)
                .HasMaxLength(2)
                .HasColumnName("LIFE_INS");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.Phone)
                .HasMaxLength(24)
                .HasColumnName("PHONE");
            entity.Property(e => e.Postal)
                .HasMaxLength(12)
                .HasColumnName("POSTAL");
            entity.Property(e => e.PsUnionSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PS_UNION__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsUnionSvmSeqMrf).HasColumnName("PS_UNION__SVM_SEQ_MRF");
            entity.Property(e => e.PsUnionSvmSeqNum).HasColumnName("PS_UNION__SVM_SEQ_NUM");
            entity.Property(e => e.RetmtPickupPct)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("RETMT_PICKUP_PCT");
            entity.Property(e => e.SdiAdminPct)
                .HasColumnType("numeric(2, 2)")
                .HasColumnName("SDI_ADMIN_PCT");
            entity.Property(e => e.SickPlan)
                .HasMaxLength(6)
                .HasColumnName("SICK_PLAN");
            entity.Property(e => e.State)
                .HasMaxLength(6)
                .HasColumnName("STATE");
            entity.Property(e => e.StdHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("STD_HOURS");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
            entity.Property(e => e.UnionLocalFlg)
                .HasMaxLength(1)
                .HasColumnName("UNION_LOCAL_FLG");
            entity.Property(e => e.UnionStewardName)
                .HasMaxLength(50)
                .HasColumnName("UNION_STEWARD_NAME");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.VacationPlan)
                .HasMaxLength(6)
                .HasColumnName("VACATION_PLAN");
            entity.Property(e => e.WorkDayHours)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("WORK_DAY_HOURS");
        });

        modelBuilder.Entity<PsVisaPmtDataV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PS_VISA_PMT_DATA_V");

            entity.Property(e => e.Country)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DependentId)
                .HasMaxLength(2)
                .HasColumnName("DEPENDENT_ID");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.DtIssued).HasColumnName("DT_ISSUED");
            entity.Property(e => e.DurationTime)
                .HasColumnType("numeric(5, 1)")
                .HasColumnName("DURATION_TIME");
            entity.Property(e => e.DurationType)
                .HasMaxLength(1)
                .HasColumnName("DURATION_TYPE");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EntryDt).HasColumnName("ENTRY_DT");
            entity.Property(e => e.ExpiratnDt).HasColumnName("EXPIRATN_DT");
            entity.Property(e => e.IssuingAuthority)
                .HasMaxLength(50)
                .HasColumnName("ISSUING_AUTHORITY");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PlaceIssued)
                .HasMaxLength(30)
                .HasColumnName("PLACE_ISSUED");
            entity.Property(e => e.StatusDt).HasColumnName("STATUS_DT");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.VisaPermitType)
                .HasMaxLength(3)
                .HasColumnName("VISA_PERMIT_TYPE");
            entity.Property(e => e.VisaWrkpmtNbr)
                .HasMaxLength(15)
                .HasColumnName("VISA_WRKPMT_NBR");
            entity.Property(e => e.VisaWrkpmtStatus)
                .HasMaxLength(1)
                .HasColumnName("VISA_WRKPMT_STATUS");
        });

        modelBuilder.Entity<PsxlatitemV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PSXLATITEM_V");

            entity.HasIndex(e => new { e.Fieldname, e.EffStatus }, "ucpathods_psxlatitem_v_fieldname_and_status");

            entity.Property(e => e.CrBtDtm).HasColumnName("CR_BT_DTM");
            entity.Property(e => e.CrBtNbr).HasColumnName("CR_BT_NBR");
            entity.Property(e => e.DmlInd)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("DML_IND");
            entity.Property(e => e.EffStatus)
                .HasMaxLength(1)
                .HasColumnName("EFF_STATUS");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Fieldname)
                .HasMaxLength(18)
                .HasColumnName("FIELDNAME");
            entity.Property(e => e.Fieldvalue)
                .HasMaxLength(4)
                .HasColumnName("FIELDVALUE");
            entity.Property(e => e.Lastupddttm).HasColumnName("LASTUPDDTTM");
            entity.Property(e => e.Lastupdoprid)
                .HasMaxLength(30)
                .HasColumnName("LASTUPDOPRID");
            entity.Property(e => e.OdsVrsnNbr).HasColumnName("ODS_VRSN_NBR");
            entity.Property(e => e.PsxSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("PSX__SVM_IS_MOSTRECENT");
            entity.Property(e => e.PsxSvmSeqMrf).HasColumnName("PSX__SVM_SEQ_MRF");
            entity.Property(e => e.PsxSvmSeqNum).HasColumnName("PSX__SVM_SEQ_NUM");
            entity.Property(e => e.Syncid)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("SYNCID");
            entity.Property(e => e.UpdBtDtm).HasColumnName("UPD_BT_DTM");
            entity.Property(e => e.UpdBtNbr).HasColumnName("UPD_BT_NBR");
            entity.Property(e => e.Xlatlongname)
                .HasMaxLength(30)
                .HasColumnName("XLATLONGNAME");
            entity.Property(e => e.Xlatshortname)
                .HasMaxLength(10)
                .HasColumnName("XLATSHORTNAME");
        });

        modelBuilder.Entity<ServiceCredit>(entity =>
        {
            entity.HasKey(e => e.ServiceCreditRecordId);

            entity.ToTable("serviceCredit");

            entity.HasIndex(e => e.ServiceCreditMonthOffset, "attainedDate");

            entity.HasIndex(e => new { e.ServiceCreditHomeDept, e.ServiceCreditMonthOffset }, "dept_offset");

            entity.HasIndex(e => e.ServiceCreditHomeDept, "home_dept");

            entity.HasIndex(e => e.ServiceCreditRecordId, "recordID");

            entity.HasIndex(e => e.ServiceCreditSchoolId, "schoolID");

            entity.HasIndex(e => new { e.ServiceCreditSchoolId, e.ServiceCreditMonthOffset }, "school_offset");

            entity.Property(e => e.ServiceCreditRecordId).HasColumnName("serviceCredit_record_ID");
            entity.Property(e => e.ServiceCreditAccrualDate)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_accrual_date");
            entity.Property(e => e.ServiceCreditCaoDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_cao_display_name");
            entity.Property(e => e.ServiceCreditCaoMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_cao_mail_ID");
            entity.Property(e => e.ServiceCreditCaoMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_cao_mothra_ID");
            entity.Property(e => e.ServiceCreditCurrentAcrucode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_current_acrucode");
            entity.Property(e => e.ServiceCreditDeptName)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_dept_name");
            entity.Property(e => e.ServiceCreditEmpName)
                .HasMaxLength(26)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_emp_name");
            entity.Property(e => e.ServiceCreditEmployeeId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_employee_ID");
            entity.Property(e => e.ServiceCreditHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_home_dept");
            entity.Property(e => e.ServiceCreditLastRunDate)
                .HasColumnType("datetime")
                .HasColumnName("serviceCredit_last_run_date");
            entity.Property(e => e.ServiceCreditMonthOffset).HasColumnName("serviceCredit_month_offset");
            entity.Property(e => e.ServiceCreditNextAcrucode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_next_acrucode");
            entity.Property(e => e.ServiceCreditOriginalHireDate)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_original_hire_date");
            entity.Property(e => e.ServiceCreditProgram)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_program");
            entity.Property(e => e.ServiceCreditSchoolId)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_school_ID");
            entity.Property(e => e.ServiceCreditServiceMonths).HasColumnName("serviceCredit_service_months");
            entity.Property(e => e.ServiceCreditServiceYears).HasColumnName("serviceCredit_service_years");
            entity.Property(e => e.ServiceCreditTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("serviceCredit_title");
        });

        modelBuilder.Entity<ServiceCreditUnit>(entity =>
        {
            entity.ToTable("service_credit_units");

            entity.HasIndex(e => e.ServiceCreditDeptCode, "deptCode");

            entity.HasIndex(e => e.ServiceCreditCaoMailid, "mailID");

            entity.HasIndex(e => new { e.ServiceCreditCaoMailid, e.ServiceCreditDeptCode }, "maildID_deptCode");

            entity.Property(e => e.ServiceCreditUnitId).HasColumnName("service_credit_unit_ID");
            entity.Property(e => e.ServiceCreditCaoMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("service_credit_cao_mailid");
            entity.Property(e => e.ServiceCreditCaoMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("service_credit_cao_mothraid");
            entity.Property(e => e.ServiceCreditDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("service_credit_dept_code");
            entity.Property(e => e.ServiceCreditDeptName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("service_credit_dept_name");
        });

        modelBuilder.Entity<TimeDailyDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TIME_DAILY_D_V");

            entity.Property(e => e.AcadDayNumQtr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACAD_DAY_NUM_QTR");
            entity.Property(e => e.AcadDayNumYr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACAD_DAY_NUM_YR");
            entity.Property(e => e.AcadLastDayQtrInd)
                .HasMaxLength(3)
                .HasColumnName("ACAD_LAST_DAY_QTR_IND");
            entity.Property(e => e.AcadQtrEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACAD_QTR_END_DT_KEY");
            entity.Property(e => e.AcadQtrNm)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("ACAD_QTR_NM");
            entity.Property(e => e.AcadWkNumQtr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACAD_WK_NUM_QTR");
            entity.Property(e => e.AcadWkNumYr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACAD_WK_NUM_YR");
            entity.Property(e => e.AcadYr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACAD_YR");
            entity.Property(e => e.AcadYrQtr)
                .HasMaxLength(7)
                .IsFixedLength()
                .HasColumnName("ACAD_YR_QTR");
            entity.Property(e => e.AcadYrStr)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("ACAD_YR_STR");
            entity.Property(e => e.ClndrDayNm)
                .HasMaxLength(9)
                .HasColumnName("CLNDR_DAY_NM");
            entity.Property(e => e.ClndrDayOfMthNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CLNDR_DAY_OF_MTH_NUM");
            entity.Property(e => e.ClndrDayOfYrNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CLNDR_DAY_OF_YR_NUM");
            entity.Property(e => e.ClndrDt).HasColumnName("CLNDR_DT");
            entity.Property(e => e.ClndrHalfNm)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("CLNDR_HALF_NM");
            entity.Property(e => e.ClndrLastDayMthInd)
                .HasMaxLength(3)
                .HasColumnName("CLNDR_LAST_DAY_MTH_IND");
            entity.Property(e => e.ClndrMthEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CLNDR_MTH_END_DT_KEY");
            entity.Property(e => e.ClndrMthNm)
                .HasMaxLength(9)
                .HasColumnName("CLNDR_MTH_NM");
            entity.Property(e => e.ClndrMthOfYrNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CLNDR_MTH_OF_YR_NUM");
            entity.Property(e => e.ClndrQtrNm)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("CLNDR_QTR_NM");
            entity.Property(e => e.ClndrWkOfYrNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CLNDR_WK_OF_YR_NUM");
            entity.Property(e => e.ClndrYr)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("CLNDR_YR");
            entity.Property(e => e.ClndrYrHalf)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("CLNDR_YR_HALF");
            entity.Property(e => e.ClndrYrMth)
                .HasMaxLength(7)
                .IsFixedLength()
                .HasColumnName("CLNDR_YR_MTH");
            entity.Property(e => e.ClndrYrQtr)
                .HasMaxLength(7)
                .IsFixedLength()
                .HasColumnName("CLNDR_YR_QTR");
            entity.Property(e => e.ClndrYrStr)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("CLNDR_YR_STR");
            entity.Property(e => e.CmpsHldyInd)
                .HasMaxLength(3)
                .HasColumnName("CMPS_HLDY_IND");
            entity.Property(e => e.DlyTmDimKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DLY_TM_DIM_KEY");
            entity.Property(e => e.FsclDayOfPerNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FSCL_DAY_OF_PER_NUM");
            entity.Property(e => e.FsclDayOfYrNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FSCL_DAY_OF_YR_NUM");
            entity.Property(e => e.FsclHalfNm)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("FSCL_HALF_NM");
            entity.Property(e => e.FsclLastDayPerInd)
                .HasMaxLength(3)
                .HasColumnName("FSCL_LAST_DAY_PER_IND");
            entity.Property(e => e.FsclPerEndDtKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FSCL_PER_END_DT_KEY");
            entity.Property(e => e.FsclPerOfYrNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FSCL_PER_OF_YR_NUM");
            entity.Property(e => e.FsclQtrNm)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("FSCL_QTR_NM");
            entity.Property(e => e.FsclWkOfYrNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("FSCL_WK_OF_YR_NUM");
            entity.Property(e => e.FsclYr)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("FSCL_YR");
            entity.Property(e => e.FsclYrHalf)
                .HasMaxLength(13)
                .IsFixedLength()
                .HasColumnName("FSCL_YR_HALF");
            entity.Property(e => e.FsclYrPer)
                .HasMaxLength(12)
                .IsFixedLength()
                .HasColumnName("FSCL_YR_PER");
            entity.Property(e => e.FsclYrQtr)
                .HasMaxLength(12)
                .IsFixedLength()
                .HasColumnName("FSCL_YR_QTR");
            entity.Property(e => e.FsclYrStr)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("FSCL_YR_STR");
            entity.Property(e => e.HldyNm)
                .HasMaxLength(40)
                .HasColumnName("HLDY_NM");
            entity.Property(e => e.LdgrYrMth)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("LDGR_YR_MTH");
            entity.Property(e => e.LymLinearNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("LYM_LINEAR_NUM");
            entity.Property(e => e.WkndInd)
                .HasMaxLength(3)
                .HasColumnName("WKND_IND");
            entity.Property(e => e.WrkdayCnt)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("WRKDAY_CNT");
        });

        modelBuilder.Entity<TitlecodeGroup>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("titlecode_group");

            entity.Property(e => e.Group)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("group");
            entity.Property(e => e.Titlecode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("titlecode");
        });

        modelBuilder.Entity<UcdEmployeeFlagsDOverride>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UCD_EMPLOYEE_FLAGS_D_OVERRIDE");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpAcdmcStdtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_STDT_FLG");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpFlgSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("EMP_FLG__SVM_IS_MOSTRECENT");
            entity.Property(e => e.EmpFlgSvmSeqMrf).HasColumnName("EMP_FLG__SVM_SEQ_MRF");
            entity.Property(e => e.EmpFlgSvmSeqNum).HasColumnName("EMP_FLG__SVM_SEQ_NUM");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMgrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MGR_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspFloaterFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLOATER_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpSupvrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SUPVR_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<UcdEmployeeFlagsDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UCD_EMPLOYEE_FLAGS_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpAcdmcStdtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_STDT_FLG");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpFlgSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("EMP_FLG__SVM_IS_MOSTRECENT");
            entity.Property(e => e.EmpFlgSvmSeqMrf).HasColumnName("EMP_FLG__SVM_SEQ_MRF");
            entity.Property(e => e.EmpFlgSvmSeqNum).HasColumnName("EMP_FLG__SVM_SEQ_NUM");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMgrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MGR_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspFloaterFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLOATER_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpSupvrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SUPVR_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
        });

        modelBuilder.Entity<UcdFauDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UCD_FAU_D_V");

            entity.Property(e => e.Account)
                .HasMaxLength(7)
                .HasColumnName("ACCOUNT");
            entity.Property(e => e.AccountNm)
                .HasMaxLength(40)
                .HasColumnName("ACCOUNT_NM");
            entity.Property(e => e.Chart)
                .HasMaxLength(2)
                .HasColumnName("CHART");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(20)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecInsrtTs).HasColumnName("DW_REC_INSRT_TS");
            entity.Property(e => e.FundCode)
                .HasMaxLength(7)
                .HasColumnName("FUND_CODE");
            entity.Property(e => e.OrgCd)
                .HasMaxLength(4)
                .HasColumnName("ORG_CD");
            entity.Property(e => e.OrgNm)
                .HasMaxLength(40)
                .HasColumnName("ORG_NM");
            entity.Property(e => e.OrganizationCode)
                .HasMaxLength(7)
                .HasColumnName("ORGANIZATION_CODE");
            entity.Property(e => e.SubFundGroup)
                .HasMaxLength(6)
                .HasColumnName("SUB_FUND_GROUP");
        });

        modelBuilder.Entity<UcdOrganizationDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UCD_ORGANIZATION_D_V");

            entity.HasIndex(e => e.DeptCd, "ucpathods_organization_key");

            entity.Property(e => e.DdwLastUpdDt).HasColumnName("DDW_LAST_UPD_DT");
            entity.Property(e => e.DeptActDt).HasColumnName("DEPT_ACT_DT");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptEffStatus)
                .HasMaxLength(1)
                .HasColumnName("DEPT_EFF_STATUS");
            entity.Property(e => e.DeptInactDt).HasColumnName("DEPT_INACT_DT");
            entity.Property(e => e.DeptShrtTtl)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHRT_TTL");
            entity.Property(e => e.DeptTtl)
                .HasMaxLength(40)
                .HasColumnName("DEPT_TTL");
            entity.Property(e => e.DivCd)
                .HasMaxLength(6)
                .HasColumnName("DIV_CD");
            entity.Property(e => e.DivTtl)
                .HasMaxLength(40)
                .HasColumnName("DIV_TTL");
            entity.Property(e => e.LocCd)
                .HasMaxLength(1)
                .HasColumnName("LOC_CD");
            entity.Property(e => e.LocDesc)
                .HasMaxLength(30)
                .HasColumnName("LOC_DESC");
            entity.Property(e => e.OrgCd)
                .HasMaxLength(5)
                .HasColumnName("ORG_CD");
            entity.Property(e => e.OrgDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_KEY");
            entity.Property(e => e.OrgEffDt).HasColumnName("ORG_EFF_DT");
            entity.Property(e => e.OrgSrcDesc)
                .HasMaxLength(20)
                .HasColumnName("ORG_SRC_DESC");
            entity.Property(e => e.OrgTtl)
                .HasMaxLength(40)
                .HasColumnName("ORG_TTL");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivL4Cd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_L4_CD");
            entity.Property(e => e.SubDivL4Ttl)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_L4_TTL");
            entity.Property(e => e.SubDivTtl)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_TTL");
        });

        modelBuilder.Entity<UcdaptdisV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UCDAPTDIS_V");

            entity.Property(e => e.Academic)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC");
            entity.Property(e => e.AcademicBasis)
                .HasMaxLength(2)
                .HasColumnName("ACADEMIC_BASIS");
            entity.Property(e => e.AdminSchDiv)
                .HasMaxLength(2)
                .HasColumnName("ADMIN_SCH_DIV");
            entity.Property(e => e.AltHomeDept)
                .HasMaxLength(6)
                .HasColumnName("ALT_HOME_DEPT");
            entity.Property(e => e.ApptBeginDate).HasColumnName("APPT_BEGIN_DATE");
            entity.Property(e => e.ApptDept)
                .HasMaxLength(6)
                .HasColumnName("APPT_DEPT");
            entity.Property(e => e.ApptEndDate).HasColumnName("APPT_END_DATE");
            entity.Property(e => e.ApptFlsaInd)
                .HasMaxLength(1)
                .HasColumnName("APPT_FLSA_IND");
            entity.Property(e => e.ApptNum)
                .HasMaxLength(2)
                .HasColumnName("APPT_NUM");
            entity.Property(e => e.ApptOffScale)
                .HasMaxLength(1)
                .HasColumnName("APPT_OFF_SCALE");
            entity.Property(e => e.ApptPaidOver)
                .HasMaxLength(2)
                .HasColumnName("APPT_PAID_OVER");
            entity.Property(e => e.ApptRepCode)
                .HasMaxLength(1)
                .HasColumnName("APPT_REP_CODE");
            entity.Property(e => e.ApptRepCodeName)
                .HasMaxLength(9)
                .HasColumnName("APPT_REP_CODE_NAME");
            entity.Property(e => e.ApptType)
                .HasMaxLength(1)
                .HasColumnName("APPT_TYPE");
            entity.Property(e => e.ApptTypeName)
                .HasMaxLength(5)
                .HasColumnName("APPT_TYPE_NAME");
            entity.Property(e => e.ApptWosInd)
                .HasMaxLength(1)
                .HasColumnName("APPT_WOS_IND");
            entity.Property(e => e.DistDeptCode)
                .HasMaxLength(6)
                .HasColumnName("DIST_DEPT_CODE");
            entity.Property(e => e.DistDos)
                .HasMaxLength(3)
                .HasColumnName("DIST_DOS");
            entity.Property(e => e.DistFte)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("DIST_FTE");
            entity.Property(e => e.DistNum)
                .HasColumnType("numeric(2, 0)")
                .HasColumnName("DIST_NUM");
            entity.Property(e => e.DistPayrate)
                .HasColumnType("numeric(9, 4)")
                .HasColumnName("DIST_PAYRATE");
            entity.Property(e => e.DistPercent)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("DIST_PERCENT");
            entity.Property(e => e.DistStep)
                .HasMaxLength(4)
                .HasColumnName("DIST_STEP");
            entity.Property(e => e.Email)
                .HasMaxLength(60)
                .HasColumnName("EMAIL");
            entity.Property(e => e.EmpName)
                .HasMaxLength(26)
                .HasColumnName("EMP_NAME");
            entity.Property(e => e.EmpOrgAddrRlse)
                .HasMaxLength(1)
                .HasColumnName("EMP_ORG_ADDR_RLSE");
            entity.Property(e => e.EmpOrgPhoneRlse)
                .HasMaxLength(1)
                .HasColumnName("EMP_ORG_PHONE_RLSE");
            entity.Property(e => e.EmpRelCdName)
                .HasMaxLength(30)
                .HasColumnName("EMP_REL_CD_NAME");
            entity.Property(e => e.EmpRelCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_REL_CODE");
            entity.Property(e => e.EmpRepCode)
                .HasMaxLength(1)
                .HasColumnName("EMP_REP_CODE");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .HasColumnName("EMP_STATUS");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(9)
                .HasColumnName("EMPLOYEE_ID");
            entity.Property(e => e.FauAcct)
                .HasMaxLength(7)
                .HasColumnName("FAU_ACCT");
            entity.Property(e => e.FauChart)
                .HasMaxLength(1)
                .HasColumnName("FAU_CHART");
            entity.Property(e => e.FauObject)
                .HasMaxLength(4)
                .HasColumnName("FAU_OBJECT");
            entity.Property(e => e.FauOpFund)
                .HasMaxLength(6)
                .HasColumnName("FAU_OP_FUND");
            entity.Property(e => e.FauOrgCd)
                .HasMaxLength(4)
                .HasColumnName("FAU_ORG_CD");
            entity.Property(e => e.FauOrgCdLevel1)
                .HasMaxLength(4)
                .HasColumnName("FAU_ORG_CD_LEVEL1");
            entity.Property(e => e.FauOrgCdLevel2)
                .HasMaxLength(4)
                .HasColumnName("FAU_ORG_CD_LEVEL2");
            entity.Property(e => e.FauProject)
                .HasMaxLength(10)
                .HasColumnName("FAU_PROJECT");
            entity.Property(e => e.FauSubacct)
                .HasMaxLength(5)
                .HasColumnName("FAU_SUBACCT");
            entity.Property(e => e.FauSubobj)
                .HasMaxLength(3)
                .HasColumnName("FAU_SUBOBJ");
            entity.Property(e => e.FixedVarCode)
                .HasMaxLength(1)
                .HasColumnName("FIXED_VAR_CODE");
            entity.Property(e => e.Grade)
                .HasMaxLength(2)
                .HasColumnName("GRADE");
            entity.Property(e => e.HireDate).HasColumnName("HIRE_DATE");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(6)
                .HasColumnName("HOME_DEPT");
            entity.Property(e => e.HomePhone)
                .HasMaxLength(10)
                .HasColumnName("HOME_PHONE");
            entity.Property(e => e.LeaveAcrucode)
                .HasMaxLength(1)
                .HasColumnName("LEAVE_ACRUCODE");
            entity.Property(e => e.Location)
                .HasMaxLength(6)
                .HasColumnName("LOCATION");
            entity.Property(e => e.NextSalaryRev)
                .HasMaxLength(1)
                .HasColumnName("NEXT_SALARY_REV");
            entity.Property(e => e.NextSalrevDate).HasColumnName("NEXT_SALREV_DATE");
            entity.Property(e => e.PayBeginDate).HasColumnName("PAY_BEGIN_DATE");
            entity.Property(e => e.PayEndDate).HasColumnName("PAY_END_DATE");
            entity.Property(e => e.PayRate)
                .HasColumnType("numeric(10, 4)")
                .HasColumnName("PAY_RATE");
            entity.Property(e => e.PaySchedule)
                .HasMaxLength(2)
                .HasColumnName("PAY_SCHEDULE");
            entity.Property(e => e.PercentFulltime)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("PERCENT_FULLTIME");
            entity.Property(e => e.PersonnelPgm)
                .HasMaxLength(1)
                .HasColumnName("PERSONNEL_PGM");
            entity.Property(e => e.PersonnelPgmName)
                .HasMaxLength(7)
                .HasColumnName("PERSONNEL_PGM_NAME");
            entity.Property(e => e.RateCode)
                .HasMaxLength(1)
                .HasColumnName("RATE_CODE");
            entity.Property(e => e.SeparateDate).HasColumnName("SEPARATE_DATE");
            entity.Property(e => e.StepGrade)
                .HasMaxLength(4)
                .HasColumnName("STEP_GRADE");
            entity.Property(e => e.TitleCode)
                .HasMaxLength(4)
                .HasColumnName("TITLE_CODE");
            entity.Property(e => e.TitleNameAbbrv)
                .HasMaxLength(30)
                .HasColumnName("TITLE_NAME_ABBRV");
            entity.Property(e => e.TitleUnitCode)
                .HasMaxLength(2)
                .HasColumnName("TITLE_UNIT_CODE");
            entity.Property(e => e.UcdAdminDept)
                .HasMaxLength(6)
                .HasColumnName("UCD_ADMIN_DEPT");
            entity.Property(e => e.UcdWorkDept)
                .HasMaxLength(6)
                .HasColumnName("UCD_WORK_DEPT");
            entity.Property(e => e.WorkSchDiv)
                .HasMaxLength(2)
                .HasColumnName("WORK_SCH_DIV");
            entity.Property(e => e.WorkStudyPgm)
                .HasMaxLength(1)
                .HasColumnName("WORK_STUDY_PGM");
            entity.Property(e => e.Wosemp)
                .HasMaxLength(1)
                .HasColumnName("WOSEMP");
        });

        modelBuilder.Entity<UcpathItemNote>(entity =>
        {
            entity.HasKey(e => e.NoteId);

            entity.ToTable("UCPathItemNote");

            entity.Property(e => e.NoteId).HasColumnName("noteID");
            entity.Property(e => e.ItemId).HasColumnName("itemID");
            entity.Property(e => e.LoginId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loginID");
            entity.Property(e => e.Note)
                .IsUnicode(false)
                .HasColumnName("note");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<UcpathMissingPerson>(entity =>
        {
            entity.HasKey(e => e.PpsId);

            entity.ToTable("UCPathMissingPerson");

            entity.Property(e => e.PpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("pps_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("note");
        });

        modelBuilder.Entity<UcpathMissingPerson20190821>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UCPathMissingPerson_20190821");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("note");
            entity.Property(e => e.PpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("pps_id");
        });

        modelBuilder.Entity<UcpathOverride>(entity =>
        {
            entity.HasKey(e => e.OverrideId);

            entity.ToTable("UCPathOverride");

            entity.Property(e => e.OverrideId).HasColumnName("override_id");
            entity.Property(e => e.EffectiveDate)
                .HasColumnType("datetime")
                .HasColumnName("effective_date");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_acdmc_federation_flg");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_acdmc_flg");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_acdmc_senate_flg");
            entity.Property(e => e.EmpAcdmcStdtFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_acdmc_stdt_flg");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_faculty_flg");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ladder_rank_flg");
            entity.Property(e => e.EmpMgrFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_mgr_flg");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_msp_career_flg");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_msp_career_partialyr_flg");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_msp_casual_flg");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_msp_cntrct_flg");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_msp_flg");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_msp_senior_mgmt_flg");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("emp_pps_uid");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_career_flg");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_career_partialyr_flg");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_casual_flg");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_casual_restricted_flg");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_cntrct_flg");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_flg");
            entity.Property(e => e.EmpSspFloaterFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_floater_flg");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_ssp_per_diem_flg");
            entity.Property(e => e.EmpSupvrFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_supvr_flg");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_faculty_flg");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("emp_wosemp_flg");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
        });

        modelBuilder.Entity<UcpathVerificationItem>(entity =>
        {
            entity.ToTable("UCPathVerificationItem");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LoginId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loginID");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.VerificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("verificationType");
        });

        modelBuilder.Entity<UcpathmissingpersonBk>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ucpathmissingperson_bk");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("note");
            entity.Property(e => e.PpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("pps_id");
        });

        modelBuilder.Entity<UnionDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UNION_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
            entity.Property(e => e.UnionAddr1Txt)
                .HasMaxLength(55)
                .HasColumnName("UNION_ADDR1_TXT");
            entity.Property(e => e.UnionAddr2Txt)
                .HasMaxLength(55)
                .HasColumnName("UNION_ADDR2_TXT");
            entity.Property(e => e.UnionAddr3Txt)
                .HasMaxLength(55)
                .HasColumnName("UNION_ADDR3_TXT");
            entity.Property(e => e.UnionAddr4Txt)
                .HasMaxLength(55)
                .HasColumnName("UNION_ADDR4_TXT");
            entity.Property(e => e.UnionBargUnitCd)
                .HasMaxLength(4)
                .HasColumnName("UNION_BARG_UNIT_CD");
            entity.Property(e => e.UnionCallbackMinHrsQty)
                .HasColumnType("numeric(2, 1)")
                .HasColumnName("UNION_CALLBACK_MIN_HRS_QTY");
            entity.Property(e => e.UnionCallbackRt)
                .HasColumnType("numeric(2, 1)")
                .HasColumnName("UNION_CALLBACK_RT");
            entity.Property(e => e.UnionCertfdFlg)
                .HasMaxLength(1)
                .HasColumnName("UNION_CERTFD_FLG");
            entity.Property(e => e.UnionCityNm)
                .HasMaxLength(30)
                .HasColumnName("UNION_CITY_NM");
            entity.Property(e => e.UnionClosedShopFlg)
                .HasMaxLength(1)
                .HasColumnName("UNION_CLOSED_SHOP_FLG");
            entity.Property(e => e.UnionCntctNm)
                .HasMaxLength(50)
                .HasColumnName("UNION_CNTCT_NM");
            entity.Property(e => e.UnionCntctPhNum)
                .HasMaxLength(24)
                .HasColumnName("UNION_CNTCT_PH_NUM");
            entity.Property(e => e.UnionCntrctBegDt).HasColumnName("UNION_CNTRCT_BEG_DT");
            entity.Property(e => e.UnionCntrctEndDt).HasColumnName("UNION_CNTRCT_END_DT");
            entity.Property(e => e.UnionCntryCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CNTRY_CD");
            entity.Property(e => e.UnionCntyNm)
                .HasMaxLength(30)
                .HasColumnName("UNION_CNTY_NM");
            entity.Property(e => e.UnionDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UNION_D_KEY");
            entity.Property(e => e.UnionDesc)
                .HasMaxLength(30)
                .HasColumnName("UNION_DESC");
            entity.Property(e => e.UnionDsbltyInsrncFlg)
                .HasMaxLength(1)
                .HasColumnName("UNION_DSBLTY_INSRNC_FLG");
            entity.Property(e => e.UnionEffDt).HasColumnName("UNION_EFF_DT");
            entity.Property(e => e.UnionEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("UNION_EFF_STAT_CD");
            entity.Property(e => e.UnionFicaPickupFlg)
                .HasMaxLength(1)
                .HasColumnName("UNION_FICA_PICKUP_FLG");
            entity.Property(e => e.UnionGeoCd)
                .HasMaxLength(11)
                .HasColumnName("UNION_GEO_CD");
            entity.Property(e => e.UnionHouseTypeCd)
                .HasMaxLength(2)
                .HasColumnName("UNION_HOUSE_TYPE_CD");
            entity.Property(e => e.UnionLifeInsrncAvlbltyCd)
                .HasMaxLength(2)
                .HasColumnName("UNION_LIFE_INSRNC_AVLBLTY_CD");
            entity.Property(e => e.UnionPhCntryCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_PH_CNTRY_CD");
            entity.Property(e => e.UnionPstlCd)
                .HasMaxLength(12)
                .HasColumnName("UNION_PSTL_CD");
            entity.Property(e => e.UnionRetrmntPickupPct)
                .HasColumnType("numeric(3, 2)")
                .HasColumnName("UNION_RETRMNT_PICKUP_PCT");
            entity.Property(e => e.UnionSdiPct)
                .HasColumnType("numeric(2, 2)")
                .HasColumnName("UNION_SDI_PCT");
            entity.Property(e => e.UnionShortDesc)
                .HasMaxLength(10)
                .HasColumnName("UNION_SHORT_DESC");
            entity.Property(e => e.UnionSickLeavePlanCd)
                .HasMaxLength(6)
                .HasColumnName("UNION_SICK_LEAVE_PLAN_CD");
            entity.Property(e => e.UnionStCd)
                .HasMaxLength(6)
                .HasColumnName("UNION_ST_CD");
            entity.Property(e => e.UnionStdHrsQty)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("UNION_STD_HRS_QTY");
            entity.Property(e => e.UnionStewardNm)
                .HasMaxLength(50)
                .HasColumnName("UNION_STEWARD_NM");
            entity.Property(e => e.UnionSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("UNION__SVM_IS_MOSTRECENT");
            entity.Property(e => e.UnionSvmSeqMrf).HasColumnName("UNION__SVM_SEQ_MRF");
            entity.Property(e => e.UnionSvmSeqNum).HasColumnName("UNION__SVM_SEQ_NUM");
            entity.Property(e => e.UnionUnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_UNION_CD");
            entity.Property(e => e.UnionUnionLocalFlg)
                .HasMaxLength(1)
                .HasColumnName("UNION_UNION_LOCAL_FLG");
            entity.Property(e => e.UnionVacationPlanCd)
                .HasMaxLength(6)
                .HasColumnName("UNION_VACATION_PLAN_CD");
            entity.Property(e => e.UnionWrkDayHrsQty)
                .HasColumnType("numeric(6, 2)")
                .HasColumnName("UNION_WRK_DAY_HRS_QTY");
        });

        modelBuilder.Entity<UpdateLog>(entity =>
        {
            entity.ToTable("updateLog");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PortionUpdated)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("portionUpdated");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<VisaPermitDataDV>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("VISA_PERMIT_DATA_D_V");

            entity.Property(e => e.DdwMd5Type2)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("DDW_MD5_TYPE2");
            entity.Property(e => e.DwRecInsrtDttm).HasColumnName("DW_REC_INSRT_DTTM");
            entity.Property(e => e.DwRecInsrtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_INSRT_ID");
            entity.Property(e => e.DwRecUpdtDttm).HasColumnName("DW_REC_UPDT_DTTM");
            entity.Property(e => e.DwRecUpdtId)
                .HasMaxLength(32)
                .HasColumnName("DW_REC_UPDT_ID");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.SrcSysCd)
                .HasMaxLength(8)
                .HasColumnName("SRC_SYS_CD");
            entity.Property(e => e.SrcUpdtBtDttm).HasColumnName("SRC_UPDT_BT_DTTM");
            entity.Property(e => e.VisaCntryCd)
                .HasMaxLength(3)
                .HasColumnName("VISA_CNTRY_CD");
            entity.Property(e => e.VisaDpndntId)
                .HasMaxLength(2)
                .HasColumnName("VISA_DPNDNT_ID");
            entity.Property(e => e.VisaDurtnTimeQty)
                .HasColumnType("numeric(5, 1)")
                .HasColumnName("VISA_DURTN_TIME_QTY");
            entity.Property(e => e.VisaDurtnTypeCd)
                .HasMaxLength(1)
                .HasColumnName("VISA_DURTN_TYPE_CD");
            entity.Property(e => e.VisaDurtnTypeDesc)
                .HasMaxLength(30)
                .HasColumnName("VISA_DURTN_TYPE_DESC");
            entity.Property(e => e.VisaEffDt).HasColumnName("VISA_EFF_DT");
            entity.Property(e => e.VisaEntryDt).HasColumnName("VISA_ENTRY_DT");
            entity.Property(e => e.VisaExprDt).HasColumnName("VISA_EXPR_DT");
            entity.Property(e => e.VisaIssuedDt).HasColumnName("VISA_ISSUED_DT");
            entity.Property(e => e.VisaIssuedPlNm)
                .HasMaxLength(30)
                .HasColumnName("VISA_ISSUED_PL_NM");
            entity.Property(e => e.VisaIssuingAuthtyNm)
                .HasMaxLength(50)
                .HasColumnName("VISA_ISSUING_AUTHTY_NM");
            entity.Property(e => e.VisaPermitTypeCd)
                .HasMaxLength(3)
                .HasColumnName("VISA_PERMIT_TYPE_CD");
            entity.Property(e => e.VisaPrmtDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("VISA_PRMT_D_KEY");
            entity.Property(e => e.VisaStatDt).HasColumnName("VISA_STAT_DT");
            entity.Property(e => e.VisaSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("VISA__SVM_IS_MOSTRECENT");
            entity.Property(e => e.VisaSvmSeqMrf).HasColumnName("VISA__SVM_SEQ_MRF");
            entity.Property(e => e.VisaSvmSeqNum).HasColumnName("VISA__SVM_SEQ_NUM");
            entity.Property(e => e.VisaWrkPermitNum)
                .HasMaxLength(15)
                .HasColumnName("VISA_WRK_PERMIT_NUM");
            entity.Property(e => e.VisaWrkPermitStatCd)
                .HasMaxLength(1)
                .HasColumnName("VISA_WRK_PERMIT_STAT_CD");
        });

        modelBuilder.Entity<VwAccrual>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_accruals");

            entity.Property(e => e.AccrualIdx).HasColumnName("ACCRUAL_IDX");
            entity.Property(e => e.AccrualMonth).HasColumnName("ACCRUAL_MONTH");
            entity.Property(e => e.AccrualYear).HasColumnName("ACCRUAL_YEAR");
            entity.Property(e => e.Asofdate).HasColumnName("ASOFDATE");
            entity.Property(e => e.Descr)
                .HasMaxLength(30)
                .HasColumnName("DESCR");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.PinNm)
                .HasMaxLength(18)
                .HasColumnName("PIN_NM");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.PinType)
                .HasMaxLength(2)
                .HasColumnName("PIN_TYPE");
            entity.Property(e => e.Type)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("TYPE");
            entity.Property(e => e.UcAccrLimit)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_ACCR_LIMIT");
            entity.Property(e => e.UcCurrBal)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_CURR_BAL");
            entity.Property(e => e.UcPrevBal)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("UC_PREV_BAL");
        });

        modelBuilder.Entity<VwAccrualsCdm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_accruals_cdm");

            entity.Property(e => e.AbsCalDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_CAL_D_KEY");
            entity.Property(e => e.AbsCalPymtDt).HasColumnName("ABS_CAL_PYMT_DT");
            entity.Property(e => e.AbsRsltFCalcRsltValRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_RSLT_F_CALC_RSLT_VAL_RT");
            entity.Property(e => e.AbsRsltFCalcValRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_RSLT_F_CALC_VAL_RT");
            entity.Property(e => e.AbsRsltFEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_EMP_REC_NUM");
            entity.Property(e => e.AbsRsltFSeq8Num)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ABS_RSLT_F_SEQ8_NUM");
            entity.Property(e => e.AbsRsltFUserAdjstValRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("ABS_RSLT_F_USER_ADJST_VAL_RT");
            entity.Property(e => e.AccrualIdx).HasColumnName("ACCRUAL_IDX");
            entity.Property(e => e.AccrualMonth)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACCRUAL_MONTH");
            entity.Property(e => e.AccrualYear)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ACCRUAL_YEAR");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpPrmyFullNm)
                .HasMaxLength(50)
                .HasColumnName("EMP_PRMY_FULL_NM");
            entity.Property(e => e.PinDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_D_KEY");
            entity.Property(e => e.PinDesc)
                .HasMaxLength(30)
                .HasColumnName("PIN_DESC");
            entity.Property(e => e.PinNm)
                .HasMaxLength(18)
                .HasColumnName("PIN_NM");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.PinTypeCd)
                .HasMaxLength(2)
                .HasColumnName("PIN_TYPE_CD");
            entity.Property(e => e.Type)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("TYPE");
        });

        modelBuilder.Entity<VwAllJobPosOrg>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AllJobPosOrg");

            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_D_KEY");
            entity.Property(e => e.DeptShrtTtl)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHRT_TTL");
            entity.Property(e => e.DeptTtl)
                .HasMaxLength(40)
                .HasColumnName("DEPT_TTL");
            entity.Property(e => e.EffDateActive).HasColumnName("EFF_DATE_ACTIVE");
            entity.Property(e => e.EffDt).HasColumnName("EFF_DT");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpBirthDt).HasColumnName("EMP_BIRTH_DT");
            entity.Property(e => e.EmpCtznshpCntryCd)
                .HasMaxLength(3)
                .HasColumnName("EMP_CTZNSHP_CNTRY_CD");
            entity.Property(e => e.EmpCtznshpCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_CNTRY_DESC");
            entity.Property(e => e.EmpCtznshpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_DESC");
            entity.Property(e => e.EmpCtznshpStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_CTZNSHP_STAT_CD");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpEffDt).HasColumnName("EMP_EFF_DT");
            entity.Property(e => e.EmpEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_EFF_STAT_CD");
            entity.Property(e => e.EmpExprDt).HasColumnName("EMP_EXPR_DT");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpOrigHireDt).HasColumnName("EMP_ORIG_HIRE_DT");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("EMP_PPS_UID");
            entity.Property(e => e.EmpPrmyEthnctyGrpCd)
                .HasMaxLength(8)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_CD");
            entity.Property(e => e.EmpPrmyEthnctyGrpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_DESC");
            entity.Property(e => e.EmpPrmyFirstNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_FIRST_NM");
            entity.Property(e => e.EmpPrmyFullNm)
                .HasMaxLength(50)
                .HasColumnName("EMP_PRMY_FULL_NM");
            entity.Property(e => e.EmpPrmyLastNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_LAST_NM");
            entity.Property(e => e.EmpPrmyMidNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_MID_NM");
            entity.Property(e => e.EmpSexCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_SEX_CD");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.EmpWrkEmailAddrTxt)
                .HasMaxLength(70)
                .HasColumnName("EMP_WRK_EMAIL_ADDR_TXT");
            entity.Property(e => e.ExpctdEndDate).HasColumnName("EXPCTD_END_DATE");
            entity.Property(e => e.Isprimary).HasColumnName("ISPRIMARY");
            entity.Property(e => e.JobActnCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_ACTN_CD");
            entity.Property(e => e.JobActnDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_ACTN_DESC");
            entity.Property(e => e.JobActnEffDt).HasColumnName("JOB_ACTN_EFF_DT");
            entity.Property(e => e.JobActnRsnCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_ACTN_RSN_CD");
            entity.Property(e => e.JobActnRsnDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_ACTN_RSN_DESC");
            entity.Property(e => e.JobCd)
                .HasMaxLength(6)
                .HasColumnName("JOB_CD");
            entity.Property(e => e.JobCdDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_CD_D_KEY");
            entity.Property(e => e.JobCdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_DESC");
            entity.Property(e => e.JobCdEffDt).HasColumnName("JOB_CD_EFF_DT");
            entity.Property(e => e.JobCdEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_EFF_STAT_CD");
            entity.Property(e => e.JobCdFlsaStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_FLSA_STAT_CD");
            entity.Property(e => e.JobCdOcuptnlSubgrpCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_OCUPTNL_SUBGRP_CD");
            entity.Property(e => e.JobCdOcuptnlSubgrpDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_OCUPTNL_SUBGRP_DESC");
            entity.Property(e => e.JobCdShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOB_CD_SHORT_DESC");
            entity.Property(e => e.JobCdUnionCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_UNION_CD");
            entity.Property(e => e.JobCdUnionDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_UNION_DESC");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobFAnnlRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_ANNL_RT");
            entity.Property(e => e.JobFEffSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_EFF_SEQ_NUM");
            entity.Property(e => e.JobFEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_EMP_REC_NUM");
            entity.Property(e => e.JobFFtePct)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("JOB_F_FTE_PCT");
            entity.Property(e => e.JobFHrlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("JOB_F_HRLY_RT");
            entity.Property(e => e.JobFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_KEY");
            entity.Property(e => e.JobFMthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_MTHLY_RT");
            entity.Property(e => e.JobFSvmIsMostrecent)
                .HasMaxLength(2)
                .HasColumnName("JOB_F__SVM_IS_MOSTRECENT");
            entity.Property(e => e.JobFSvmPrimaryIdx)
                .HasMaxLength(2)
                .HasColumnName("JOB_F__SVM_PRIMARY_IDX");
            entity.Property(e => e.JobFSvmSeqNum).HasColumnName("JOB_F__SVM_SEQ_NUM");
            entity.Property(e => e.JobStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_STAT_CD");
            entity.Property(e => e.JobStatDEmpKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_EMP_KEY");
            entity.Property(e => e.JobStatDHrKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_HR_KEY");
            entity.Property(e => e.JobStatDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_KEY");
            entity.Property(e => e.JobStatDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STAT_DESC");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.OrgDJobCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_JOB_CUR_KEY");
            entity.Property(e => e.OrgDJobKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_JOB_KEY");
            entity.Property(e => e.OrgDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_KEY");
            entity.Property(e => e.PosnClassInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_CLASS_IND");
            entity.Property(e => e.PosnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_D_KEY");
            entity.Property(e => e.PosnDeptCd)
                .HasMaxLength(10)
                .HasColumnName("POSN_DEPT_CD");
            entity.Property(e => e.PosnDeptDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_DEPT_DESC");
            entity.Property(e => e.PosnDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_DESC");
            entity.Property(e => e.PosnEffDt).HasColumnName("POSN_EFF_DT");
            entity.Property(e => e.PosnEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("POSN_EFF_STAT_CD");
            entity.Property(e => e.PosnGrdCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_GRD_CD");
            entity.Property(e => e.PosnJobCd)
                .HasMaxLength(6)
                .HasColumnName("POSN_JOB_CD");
            entity.Property(e => e.PosnJobCdDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_JOB_CD_DESC");
            entity.Property(e => e.PosnNum)
                .HasMaxLength(8)
                .HasColumnName("POSN_NUM");
            entity.Property(e => e.PosnUnionCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_UNION_CD");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivTtl)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_TTL");
            entity.Property(e => e.UnionDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UNION_D_KEY");
        });

        modelBuilder.Entity<VwEmpJobPosOrg>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_EmpJobPosOrg");

            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("DEPT_D_KEY");
            entity.Property(e => e.DeptShrtTtl)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHRT_TTL");
            entity.Property(e => e.DeptTtl)
                .HasMaxLength(40)
                .HasColumnName("DEPT_TTL");
            entity.Property(e => e.EffDateActive).HasColumnName("EFF_DATE_ACTIVE");
            entity.Property(e => e.EffDt).HasColumnName("EFF_DT");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpBirthDt).HasColumnName("EMP_BIRTH_DT");
            entity.Property(e => e.EmpCtznshpCntryCd)
                .HasMaxLength(3)
                .HasColumnName("EMP_CTZNSHP_CNTRY_CD");
            entity.Property(e => e.EmpCtznshpCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_CNTRY_DESC");
            entity.Property(e => e.EmpCtznshpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_DESC");
            entity.Property(e => e.EmpCtznshpStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_CTZNSHP_STAT_CD");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpEffDt).HasColumnName("EMP_EFF_DT");
            entity.Property(e => e.EmpEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_EFF_STAT_CD");
            entity.Property(e => e.EmpExprDt).HasColumnName("EMP_EXPR_DT");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpOrigHireDt).HasColumnName("EMP_ORIG_HIRE_DT");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("EMP_PPS_UID");
            entity.Property(e => e.EmpPrmyEthnctyGrpCd)
                .HasMaxLength(8)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_CD");
            entity.Property(e => e.EmpPrmyEthnctyGrpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_DESC");
            entity.Property(e => e.EmpPrmyFirstNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_FIRST_NM");
            entity.Property(e => e.EmpPrmyFullNm)
                .HasMaxLength(50)
                .HasColumnName("EMP_PRMY_FULL_NM");
            entity.Property(e => e.EmpPrmyLastNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_LAST_NM");
            entity.Property(e => e.EmpPrmyMidNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_MID_NM");
            entity.Property(e => e.EmpSexCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_SEX_CD");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.EmpWrkEmailAddrTxt)
                .HasMaxLength(70)
                .HasColumnName("EMP_WRK_EMAIL_ADDR_TXT");
            entity.Property(e => e.ExpctdEndDate).HasColumnName("EXPCTD_END_DATE");
            entity.Property(e => e.Isprimary).HasColumnName("ISPRIMARY");
            entity.Property(e => e.JobCd)
                .HasMaxLength(6)
                .HasColumnName("JOB_CD");
            entity.Property(e => e.JobCdDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_DESC");
            entity.Property(e => e.JobCdFlsaStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_CD_FLSA_STAT_CD");
            entity.Property(e => e.JobCdOcuptnlSubgrpCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_OCUPTNL_SUBGRP_CD");
            entity.Property(e => e.JobCdOcuptnlSubgrpDesc)
                .HasMaxLength(50)
                .HasColumnName("JOB_CD_OCUPTNL_SUBGRP_DESC");
            entity.Property(e => e.JobCdShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOB_CD_SHORT_DESC");
            entity.Property(e => e.JobCdUnionCd)
                .HasMaxLength(3)
                .HasColumnName("JOB_CD_UNION_CD");
            entity.Property(e => e.JobCdUnionDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_CD_UNION_DESC");
            entity.Property(e => e.JobDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_D_KEY");
            entity.Property(e => e.JobFAnnlRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_ANNL_RT");
            entity.Property(e => e.JobFEffSeqNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_EFF_SEQ_NUM");
            entity.Property(e => e.JobFEmpRecNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_EMP_REC_NUM");
            entity.Property(e => e.JobFFtePct)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("JOB_F_FTE_PCT");
            entity.Property(e => e.JobFHrlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("JOB_F_HRLY_RT");
            entity.Property(e => e.JobFKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_F_KEY");
            entity.Property(e => e.JobFMthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("JOB_F_MTHLY_RT");
            entity.Property(e => e.JobFSvmPrimaryIdx)
                .HasMaxLength(2)
                .HasColumnName("JOB_F__SVM_PRIMARY_IDX");
            entity.Property(e => e.JobStatCd)
                .HasMaxLength(1)
                .HasColumnName("JOB_STAT_CD");
            entity.Property(e => e.JobStatDEmpKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_EMP_KEY");
            entity.Property(e => e.JobStatDHrKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("JOB_STAT_D_HR_KEY");
            entity.Property(e => e.JobStatDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STAT_DESC");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.OrgDJobCurKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_JOB_CUR_KEY");
            entity.Property(e => e.OrgDJobKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORG_D_JOB_KEY");
            entity.Property(e => e.PosnClassInd)
                .HasMaxLength(1)
                .HasColumnName("POSN_CLASS_IND");
            entity.Property(e => e.PosnDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("POSN_D_KEY");
            entity.Property(e => e.PosnDeptCd)
                .HasMaxLength(10)
                .HasColumnName("POSN_DEPT_CD");
            entity.Property(e => e.PosnDeptDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_DEPT_DESC");
            entity.Property(e => e.PosnDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_DESC");
            entity.Property(e => e.PosnEffDt).HasColumnName("POSN_EFF_DT");
            entity.Property(e => e.PosnEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("POSN_EFF_STAT_CD");
            entity.Property(e => e.PosnGrdCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_GRD_CD");
            entity.Property(e => e.PosnJobCd)
                .HasMaxLength(6)
                .HasColumnName("POSN_JOB_CD");
            entity.Property(e => e.PosnJobCdDesc)
                .HasMaxLength(30)
                .HasColumnName("POSN_JOB_CD_DESC");
            entity.Property(e => e.PosnNum)
                .HasMaxLength(8)
                .HasColumnName("POSN_NUM");
            entity.Property(e => e.PosnUnionCd)
                .HasMaxLength(3)
                .HasColumnName("POSN_UNION_CD");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivTtl)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_TTL");
            entity.Property(e => e.UnionDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("UNION_D_KEY");
        });

        modelBuilder.Entity<VwEmployee>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Employee");

            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpCtznshpCntryCd)
                .HasMaxLength(3)
                .HasColumnName("EMP_CTZNSHP_CNTRY_CD");
            entity.Property(e => e.EmpCtznshpCntryDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_CNTRY_DESC");
            entity.Property(e => e.EmpCtznshpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_CTZNSHP_DESC");
            entity.Property(e => e.EmpCtznshpStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_CTZNSHP_STAT_CD");
            entity.Property(e => e.EmpDKey)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMP_D_KEY");
            entity.Property(e => e.EmpEffDt).HasColumnName("EMP_EFF_DT");
            entity.Property(e => e.EmpEffStatCd)
                .HasMaxLength(1)
                .HasColumnName("EMP_EFF_STAT_CD");
            entity.Property(e => e.EmpExprDt).HasColumnName("EMP_EXPR_DT");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpId)
                .HasMaxLength(11)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpPpsUid)
                .HasMaxLength(9)
                .HasColumnName("EMP_PPS_UID");
            entity.Property(e => e.EmpPrmyEthnctyGrpCd)
                .HasMaxLength(8)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_CD");
            entity.Property(e => e.EmpPrmyEthnctyGrpDesc)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_ETHNCTY_GRP_DESC");
            entity.Property(e => e.EmpPrmyFirstNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_FIRST_NM");
            entity.Property(e => e.EmpPrmyFullNm)
                .HasMaxLength(50)
                .HasColumnName("EMP_PRMY_FULL_NM");
            entity.Property(e => e.EmpPrmyLastNm)
                .HasMaxLength(30)
                .HasColumnName("EMP_PRMY_LAST_NM");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.EmpWrkEmailAddrTxt)
                .HasMaxLength(70)
                .HasColumnName("EMP_WRK_EMAIL_ADDR_TXT");
        });

        modelBuilder.Entity<VwJobCodeAndGroup>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_JobCodeAndGroup");

            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.JobcodeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOBCODE_DESC");
            entity.Property(e => e.JobcodeShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOBCODE_SHORT_DESC");
            entity.Property(e => e.JobcodeUnionCode)
                .HasMaxLength(3)
                .HasColumnName("JOBCODE_UNION_CODE");
            entity.Property(e => e.Jobgroup)
                .HasMaxLength(3)
                .HasColumnName("JOBGROUP");
            entity.Property(e => e.JobgroupDesc)
                .HasMaxLength(50)
                .HasColumnName("JOBGROUP_DESC");
        });

        modelBuilder.Entity<VwLoa>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LOA");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDescription)
                .HasMaxLength(50)
                .HasColumnName("ACTION_DESCRIPTION");
            entity.Property(e => e.ActionReason)
                .HasMaxLength(3)
                .HasColumnName("ACTION_REASON");
            entity.Property(e => e.BgnDt).HasColumnName("BGN_DT");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.EndDt).HasColumnName("END_DT");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.LastDateWorked).HasColumnName("LAST_DATE_WORKED");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.PayBeginDt).HasColumnName("PAY_BEGIN_DT");
            entity.Property(e => e.PayEndDt).HasColumnName("PAY_END_DT");
            entity.Property(e => e.ReasonDescription)
                .HasMaxLength(30)
                .HasColumnName("REASON_DESCRIPTION");
            entity.Property(e => e.ReturnDt).HasColumnName("RETURN_DT");
            entity.Property(e => e.StatusDescription)
                .HasMaxLength(30)
                .HasColumnName("STATUS_DESCRIPTION");
            entity.Property(e => e.UcAbsReason)
                .HasMaxLength(3)
                .HasColumnName("UC_ABS_REASON");
            entity.Property(e => e.UcPayReturnDt).HasColumnName("UC_PAY_RETURN_DT");
            entity.Property(e => e.UcReasonDescription)
                .HasMaxLength(29)
                .IsUnicode(false)
                .HasColumnName("UC_REASON_DESCRIPTION");
            entity.Property(e => e.WfStatus)
                .HasMaxLength(1)
                .HasColumnName("WF_STATUS");
        });

        modelBuilder.Entity<VwPerson>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Person");

            entity.Property(e => e.Birthdate).HasColumnName("BIRTHDATE");
            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(30)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.CitizenshipStatusCode)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS_CODE");
            entity.Property(e => e.Country)
                .HasMaxLength(30)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(70)
                .HasColumnName("EMAIL_ADDR");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpAcdmcStdtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_STDT_FLG");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMgrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MGR_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspFloaterFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLOATER_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpSupvrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SUPVR_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .HasColumnName("MIDDLE_NAME");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OrigHireDt).HasColumnName("ORIG_HIRE_DT");
            entity.Property(e => e.PpsId)
                .HasMaxLength(254)
                .HasColumnName("PPS_ID");
        });

        modelBuilder.Entity<VwPersonAccrual>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PersonAccruals");

            entity.Property(e => e.AccrualDesc)
                .HasMaxLength(30)
                .HasColumnName("ACCRUAL_DESC");
            entity.Property(e => e.AccrualIdx).HasColumnName("ACCRUAL_IDX");
            entity.Property(e => e.AccrualMonth).HasColumnName("ACCRUAL_MONTH");
            entity.Property(e => e.AccrualYear).HasColumnName("ACCRUAL_YEAR");
            entity.Property(e => e.Asofdate).HasColumnName("ASOFDATE");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OrderNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ORDER_NUM");
            entity.Property(e => e.PinCode)
                .HasMaxLength(22)
                .HasColumnName("PIN_CODE");
            entity.Property(e => e.PinNm)
                .HasMaxLength(18)
                .HasColumnName("PIN_NM");
            entity.Property(e => e.PinNum)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("PIN_NUM");
            entity.Property(e => e.PinType)
                .HasMaxLength(2)
                .HasColumnName("PIN_TYPE");
            entity.Property(e => e.Type)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("TYPE");
            entity.Property(e => e.UcAccrLimit)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_ACCR_LIMIT");
            entity.Property(e => e.UcAprMaxInd)
                .HasMaxLength(1)
                .HasColumnName("UC_APR_MAX_IND");
            entity.Property(e => e.UcCurrBal)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_CURR_BAL");
            entity.Property(e => e.UcPrdAccrual)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_PRD_ACCRUAL");
            entity.Property(e => e.UcPrdAdjusted)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_PRD_ADJUSTED");
            entity.Property(e => e.UcPrdTaken)
                .HasColumnType("numeric(14, 2)")
                .HasColumnName("UC_PRD_TAKEN");
            entity.Property(e => e.UcPrevBal)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("UC_PREV_BAL");
        });

        modelBuilder.Entity<VwPersonJobPosition>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PersonJobPosition");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDt).HasColumnName("ACTION_DT");
            entity.Property(e => e.AnnualRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ANNUAL_RT");
            entity.Property(e => e.Birthdate).HasColumnName("BIRTHDATE");
            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(30)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.CitizenshipStatusCode)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS_CODE");
            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.Country)
                .HasMaxLength(30)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptDesc)
                .HasMaxLength(40)
                .HasColumnName("DEPT_DESC");
            entity.Property(e => e.DeptShortDesc)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHORT_DESC");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.EffDateActive).HasColumnName("EFF_DATE_ACTIVE");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFFSEQ");
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(70)
                .HasColumnName("EMAIL_ADDR");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.EmplStatus)
                .HasMaxLength(1)
                .HasColumnName("EMPL_STATUS");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.ExpectedEndDate).HasColumnName("EXPECTED_END_DATE");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.Fte)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("FTE");
            entity.Property(e => e.Grade)
                .HasMaxLength(3)
                .HasColumnName("GRADE");
            entity.Property(e => e.HourlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("HOURLY_RT");
            entity.Property(e => e.Isprimary).HasColumnName("ISPRIMARY");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(1)
                .HasColumnName("JOB_STATUS");
            entity.Property(e => e.JobStatus1)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("JobStatus");
            entity.Property(e => e.JobStatusDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STATUS_DESC");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.JobcodeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOBCODE_DESC");
            entity.Property(e => e.JobcodeShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOBCODE_SHORT_DESC");
            entity.Property(e => e.JobcodeUnionCode)
                .HasMaxLength(3)
                .HasColumnName("JOBCODE_UNION_CODE");
            entity.Property(e => e.Jobgroup)
                .HasMaxLength(3)
                .HasColumnName("JOBGROUP");
            entity.Property(e => e.JobgroupDesc)
                .HasMaxLength(50)
                .HasColumnName("JOBGROUP_DESC");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.ManuallySeparated)
                .HasMaxLength(2)
                .HasColumnName("MANUALLY_SEPARATED");
            entity.Property(e => e.MonthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("MONTHLY_RT");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OrigHireDt).HasColumnName("ORIG_HIRE_DT");
            entity.Property(e => e.PositionAction)
                .HasMaxLength(3)
                .HasColumnName("POSITION_ACTION");
            entity.Property(e => e.PositionActionDt).HasColumnName("POSITION_ACTION_DT");
            entity.Property(e => e.PositionDeptDesc)
                .HasMaxLength(40)
                .HasColumnName("POSITION_DEPT_DESC");
            entity.Property(e => e.PositionDeptShortDesc)
                .HasMaxLength(15)
                .HasColumnName("POSITION_DEPT_SHORT_DESC");
            entity.Property(e => e.PositionDeptid)
                .HasMaxLength(10)
                .HasColumnName("POSITION_DEPTID");
            entity.Property(e => e.PositionDesc)
                .HasMaxLength(30)
                .HasColumnName("POSITION_DESC");
            entity.Property(e => e.PositionEffdt).HasColumnName("POSITION_EFFDT");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PositionShortDesc)
                .HasMaxLength(10)
                .HasColumnName("POSITION_SHORT_DESC");
            entity.Property(e => e.PositionStatus)
                .HasMaxLength(1)
                .HasColumnName("POSITION_STATUS");
            entity.Property(e => e.PpsId)
                .HasMaxLength(254)
                .HasColumnName("PPS_ID");
            entity.Property(e => e.Primaryindex)
                .HasMaxLength(2)
                .HasColumnName("PRIMARYINDEX");
            entity.Property(e => e.ReportsTo)
                .HasMaxLength(8)
                .HasColumnName("REPORTS_TO");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivDesc)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_DESC");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
        });

        modelBuilder.Entity<VwPersonJobPositionAll>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PersonJobPositionAll");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDescr)
                .HasMaxLength(50)
                .HasColumnName("ACTION_DESCR");
            entity.Property(e => e.ActionDt).HasColumnName("ACTION_DT");
            entity.Property(e => e.AnnualRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ANNUAL_RT");
            entity.Property(e => e.Birthdate).HasColumnName("BIRTHDATE");
            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(30)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.CitizenshipStatusCode)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS_CODE");
            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.Country)
                .HasMaxLength(30)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptDesc)
                .HasMaxLength(40)
                .HasColumnName("DEPT_DESC");
            entity.Property(e => e.DeptShortDesc)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHORT_DESC");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.EffDateActive).HasColumnName("EFF_DATE_ACTIVE");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFFSEQ");
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(70)
                .HasColumnName("EMAIL_ADDR");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.EmplStatus)
                .HasMaxLength(1)
                .HasColumnName("EMPL_STATUS");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.ExpectedEndDate).HasColumnName("EXPECTED_END_DATE");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.Fte)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("FTE");
            entity.Property(e => e.Grade)
                .HasMaxLength(3)
                .HasColumnName("GRADE");
            entity.Property(e => e.HourlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("HOURLY_RT");
            entity.Property(e => e.Isprimary).HasColumnName("ISPRIMARY");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(1)
                .HasColumnName("JOB_STATUS");
            entity.Property(e => e.JobStatus1)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("JobStatus");
            entity.Property(e => e.JobStatusDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STATUS_DESC");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.JobcodeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOBCODE_DESC");
            entity.Property(e => e.JobcodeShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOBCODE_SHORT_DESC");
            entity.Property(e => e.JobcodeUnionCode)
                .HasMaxLength(3)
                .HasColumnName("JOBCODE_UNION_CODE");
            entity.Property(e => e.Jobgroup)
                .HasMaxLength(3)
                .HasColumnName("JOBGROUP");
            entity.Property(e => e.JobgroupDesc)
                .HasMaxLength(50)
                .HasColumnName("JOBGROUP_DESC");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.ManuallySeparated)
                .HasMaxLength(2)
                .HasColumnName("MANUALLY_SEPARATED");
            entity.Property(e => e.MonthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("MONTHLY_RT");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OrigHireDt).HasColumnName("ORIG_HIRE_DT");
            entity.Property(e => e.PositionAction)
                .HasMaxLength(3)
                .HasColumnName("POSITION_ACTION");
            entity.Property(e => e.PositionActionDt).HasColumnName("POSITION_ACTION_DT");
            entity.Property(e => e.PositionDeptDesc)
                .HasMaxLength(40)
                .HasColumnName("POSITION_DEPT_DESC");
            entity.Property(e => e.PositionDeptShortDesc)
                .HasMaxLength(15)
                .HasColumnName("POSITION_DEPT_SHORT_DESC");
            entity.Property(e => e.PositionDeptid)
                .HasMaxLength(10)
                .HasColumnName("POSITION_DEPTID");
            entity.Property(e => e.PositionDesc)
                .HasMaxLength(30)
                .HasColumnName("POSITION_DESC");
            entity.Property(e => e.PositionEffdt).HasColumnName("POSITION_EFFDT");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PositionShortDesc)
                .HasMaxLength(10)
                .HasColumnName("POSITION_SHORT_DESC");
            entity.Property(e => e.PositionStatus)
                .HasMaxLength(1)
                .HasColumnName("POSITION_STATUS");
            entity.Property(e => e.Primaryindex)
                .HasMaxLength(2)
                .HasColumnName("PRIMARYINDEX");
            entity.Property(e => e.ReportsTo)
                .HasMaxLength(8)
                .HasColumnName("REPORTS_TO");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivDesc)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_DESC");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
        });

        modelBuilder.Entity<VwRankStepEthnicityPayRate>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_rankStep_ethnicity_payRate");

            entity.Property(e => e.EthsexEthnicity)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("ethsex_ethnicity");
            entity.Property(e => e.EthsexPayRate)
                .HasColumnType("numeric(12, 4)")
                .HasColumnName("ethsex_payRate");
            entity.Property(e => e.RankStep)
                .HasMaxLength(42)
                .IsUnicode(false)
                .HasColumnName("Rank_Step");
        });

        modelBuilder.Entity<VwStipend>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_stipends");

            entity.Property(e => e.AddlPayFrequency)
                .HasMaxLength(1)
                .HasColumnName("ADDL_PAY_FREQUENCY");
            entity.Property(e => e.AddlSeq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("ADDL_SEQ");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.EarningsEndDt).HasColumnName("EARNINGS_END_DT");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.Erncd)
                .HasMaxLength(3)
                .HasColumnName("ERNCD");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.NameDisplay)
                .HasMaxLength(60)
                .HasColumnName("NAME_DISPLAY");
            entity.Property(e => e.OthPay)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("OTH_PAY");
            entity.Property(e => e.PayPeriod1)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD1");
            entity.Property(e => e.PayPeriod2)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD2");
            entity.Property(e => e.PayPeriod3)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD3");
            entity.Property(e => e.PayPeriod4)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD4");
            entity.Property(e => e.PayPeriod5)
                .HasMaxLength(1)
                .HasColumnName("PAY_PERIOD5");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PsAddlPaySvmSeqMrf).HasColumnName("PS_ADDL_PAY__SVM_SEQ_MRF");
            entity.Property(e => e.PsAddlPaySvmSeqNum).HasColumnName("PS_ADDL_PAY__SVM_SEQ_NUM");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
