using Microsoft.EntityFrameworkCore;

namespace Rxintel.Generic.Entities.Entities
{
    public class RxintelDbContext : DbContext
    {
        public RxintelDbContext(DbContextOptions<RxintelDbContext> options) : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Pharmacy> Pharmacies { get; set; }
        public DbSet<PharmacyUser> PharmacyUsers { get; set; }
        public DbSet<InsuranceGroup> InsuranceGroups { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<NDCGroup> NDCGroups { get; set; }
        public DbSet<Medicine> MedicineMasters { get; set; }
        public DbSet<Wholesaler> Wholesalers { get; set; }
        public DbSet<PharmacyWholesaler> PharmacyWholesalers { get; set; }
        public DbSet<BillingFieldMapping> BillingFieldMappings { get; set; }
        public DbSet<WholesalerFieldMapping> WholesalerFieldMappings { get; set; }
        public DbSet<Billing> Billings { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<PharmacyBillingSoftware> PharmacyBillingSoftwares { get; set; }
        public DbSet<ImportErrorFile> ImportErrorFiles { get; set; }
        public DbSet<ImportErrorDetail> ImportErrorDetails { get; set; }
        public DbSet<ErxAuditBatch> ErxAuditBatches { get; set; }
        public DbSet<ErxAuditRecord> ErxAuditRecords { get; set; }
        public DbSet<InsuranceCompany> InsuranceCompanies { get; set; }
        public DbSet<ErxColumnMatchRule> ErxColumnMatchRules { get; set; }
        public DbSet<ErxAuditDiscrepancy> ErxAuditDiscrepancies { get; set; }
        public DbSet<ErxAuditAlertLog> ErxAuditAlertLogs { get; set; }
        public DbSet<ErxSigMapping> ErxSigMappings { get; set; }
        public DbSet<TermsAndConditions> TermsAndConditions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InsuranceGroup>()
                .HasOne(x => x.ChildInsurance)
                .WithMany()
                .HasForeignKey(x => x.ChildInsuranceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InsuranceGroup>()
                .HasIndex(x => x.BINNumber);
            // NDCGroup
            modelBuilder.Entity<NDCGroup>()
                .HasOne(x => x.ParentMedicine)
                .WithMany()
                .HasForeignKey(x => x.ParentMedicineId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NDCGroup>()
                .HasOne(x => x.ChildMedicine)
                .WithMany()
                .HasForeignKey(x => x.ChildMedicineId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PharmacyWholesaler>()
                .HasOne(x => x.Pharmacy)
                .WithMany()
                .HasForeignKey(x => x.PharmacyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PharmacyWholesaler>()
                .HasOne(x => x.Wholesaler)
                .WithMany()
                .HasForeignKey(x => x.WholesalerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TermsAndConditions>(entity =>
            {
                entity.HasOne(x => x.UpdatedByAdmin)
                    .WithMany()
                    .HasForeignKey(x => x.UpdatedByAdminId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Pharmacy>()
                .HasOne(x => x.State)
                .WithMany()
                .HasForeignKey(x => x.StateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Pharmacy>()
                .HasOne(x => x.PharmacyBillingSoftware)
                .WithMany()
                .HasForeignKey(x => x.PharmacySoftwareId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ImportErrorFile>()
                .HasOne(x => x.Pharmacy)
                .WithMany()
                .HasForeignKey(x => x.PharmacyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ImportErrorDetail>()
                .HasOne(x => x.File)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ImportErrorDetail>()
                .HasIndex(x => x.FileId);

            modelBuilder.Entity<Billing>()
                .HasOne(x => x.ImportErrorFile)
                .WithMany()
                .HasForeignKey(x => x.ImportErrorFileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Billing>()
                .HasIndex(x => x.ImportErrorFileId);

            modelBuilder.Entity<Purchase>()
                .HasOne(x => x.ImportErrorFile)
                .WithMany()
                .HasForeignKey(x => x.ImportErrorFileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Purchase>()
                .HasIndex(x => x.ImportErrorFileId);

            modelBuilder.Entity<Medicine>(entity =>
            {
                entity.Property(x => x.NormalizedNdc)
                    .HasMaxLength(100)
                    .HasComputedColumnSql("CAST(REPLACE([NDC], N'-', N'') AS nvarchar(100))", stored: true);

                // Non-unique: product CSV can have multiple package sizes per PRODUCTNDC.
                entity.HasIndex(x => x.NDC)
                    .HasDatabaseName("IX_Medicine_NDC");

                entity.HasIndex(x => x.NormalizedNdc)
                    .HasDatabaseName("IX_Medicine_NormalizedNdc");

                entity.HasIndex(x => x.Brand)
                    .HasDatabaseName("IX_Medicine_Brand");
            });

            modelBuilder.Entity<Billing>()
                .HasOne(x => x.SecondaryInsuranceProvider)
                .WithMany()
                .HasForeignKey(x => x.SecondaryInsuranceId)
                .OnDelete(DeleteBehavior.NoAction);

            // ERx Audit Entities Configuration
            modelBuilder.Entity<ErxAuditBatch>(entity =>
            {
                entity.HasIndex(x => x.UploadedAt);
                entity.HasIndex(x => x.PharmacyId);
                entity.HasOne(x => x.Pharmacy)
                    .WithMany()
                    .HasForeignKey(x => x.PharmacyId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ErxAuditRecord>(entity =>
            {
                entity.HasKey(x => x.RecordId);
                entity.Property(x => x.RecordId)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.HasOne(x => x.Batch)
                    .WithMany(x => x.Records)
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.RxNumber);
                entity.HasIndex(x => x.BatchId);
            });

            modelBuilder.Entity<ErxColumnMatchRule>(entity =>
            {
                entity.HasOne(x => x.InsuranceCompany)
                    .WithMany()
                    .HasForeignKey(x => x.InsuranceCompanyId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasData(
                    new ErxColumnMatchRule { RuleId = 1, FieldGroupName = "RX Number", MmColumnName = "RxNumber", ErxColumnName = "ErxRxNumber", MatchType = "Exact", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 2, FieldGroupName = "Drug Name", MmColumnName = "MmDispensedDrugName", ErxColumnName = "ErxDrugName", MatchType = "Fuzzy", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 3, FieldGroupName = "NDC", MmColumnName = "MmDispensedNdc", ErxColumnName = "ErxNdc", MatchType = "Exact", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 4, FieldGroupName = "Quantity Ordered", MmColumnName = "MmQtyOrd", ErxColumnName = "ErxQtyOrd", MatchType = "NumericTolerance", ToleranceValue = 0.00m, IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 5, FieldGroupName = "Quantity Dispensed", MmColumnName = "MmQtyDispensed", ErxColumnName = "ErxQtyDispensed", MatchType = "NumericTolerance", ToleranceValue = 0.00m, IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 6, FieldGroupName = "Days Supply", MmColumnName = "MmDispensedDaysSupply", ErxColumnName = "ErxDaysSupply", MatchType = "NumericTolerance", ToleranceValue = 0.00m, IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 7, FieldGroupName = "Prescriber NPI", MmColumnName = "MmPrescriberNpi", ErxColumnName = "ErxPrescriberNpi", MatchType = "Exact", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 8, FieldGroupName = "Prescriber DEA", MmColumnName = "MmPrescriberDea", ErxColumnName = "ErxPrescriberDea", MatchType = "CaseInsensitive", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 9, FieldGroupName = "Patient Name", MmColumnName = "MmPatientName", ErxColumnName = "ErxPatientName", MatchType = "CaseInsensitive", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 10, FieldGroupName = "Patient DOB", MmColumnName = "MmPatientDob", ErxColumnName = "ErxPatientDob", MatchType = "Exact", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 11, FieldGroupName = "DAW Code", MmColumnName = "MmDawCode", ErxColumnName = "ErxDawCode", MatchType = "Exact", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxColumnMatchRule { RuleId = 12, FieldGroupName = "Sig / Directions", MmColumnName = "MmDispensedSig", ErxColumnName = "ErxSig", MatchType = "CaseInsensitive", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });

            modelBuilder.Entity<ErxAuditDiscrepancy>(entity =>
            {
                entity.HasKey(x => x.DiscrepancyId);
                entity.Property(x => x.DiscrepancyId)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.HasOne(x => x.Record)
                    .WithMany(x => x.Discrepancies)
                    .HasForeignKey(x => x.RecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Rule)
                    .WithMany(x => x.Discrepancies)
                    .HasForeignKey(x => x.RuleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.RecordId);
                entity.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<ErxAuditAlertLog>(entity =>
            {
                entity.HasKey(x => x.AlertLogId);
                entity.Property(x => x.AlertLogId)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.HasOne(x => x.Record)
                    .WithMany(x => x.AlertLogs)
                    .HasForeignKey(x => x.RecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.RecordId);
            });

            modelBuilder.Entity<ErxSigMapping>(entity =>
            {
                entity.HasIndex(x => x.Abbreviation);
                entity.HasIndex(x => x.Category);
                entity.HasIndex(x => x.IsActive);

                entity.HasData(
                    new ErxSigMapping { Id = 1, Abbreviation = "PO", SimpleEnglish = "By mouth (orally)", NormalizedToken = "by mouth", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 2, Abbreviation = "SL", SimpleEnglish = "Under the tongue (sublingually)", NormalizedToken = "sublingually", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 3, Abbreviation = "PR", SimpleEnglish = "In the rectum (rectally)", NormalizedToken = "rectally", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 4, Abbreviation = "PV", SimpleEnglish = "In the vagina (vaginally)", NormalizedToken = "vaginally", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 5, Abbreviation = "IM", SimpleEnglish = "Into the muscle (intramuscularly)", NormalizedToken = "intramuscularly", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 6, Abbreviation = "IV", SimpleEnglish = "Into the vein (intravenously)", NormalizedToken = "intravenously", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 7, Abbreviation = "SC / SQ", SimpleEnglish = "Under the skin (subcutaneously)", NormalizedToken = "subcutaneously", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 8, Abbreviation = "TOP", SimpleEnglish = "Topically (apply to skin)", NormalizedToken = "topically", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 9, Abbreviation = "INH", SimpleEnglish = "Inhale (by inhalation)", NormalizedToken = "by inhalation", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 10, Abbreviation = "OD", SimpleEnglish = "Right eye", NormalizedToken = "right eye", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 11, Abbreviation = "OS", SimpleEnglish = "Left eye", NormalizedToken = "left eye", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 12, Abbreviation = "OU", SimpleEnglish = "Both eyes", NormalizedToken = "both eyes", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 13, Abbreviation = "AD", SimpleEnglish = "Right ear", NormalizedToken = "right ear", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 14, Abbreviation = "AS", SimpleEnglish = "Left ear", NormalizedToken = "left ear", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 15, Abbreviation = "AU", SimpleEnglish = "Both ears", NormalizedToken = "both ears", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 16, Abbreviation = "QD / daily", SimpleEnglish = "Once a day", NormalizedToken = "daily", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 17, Abbreviation = "BID", SimpleEnglish = "Twice a day", NormalizedToken = "twice daily", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 18, Abbreviation = "TID", SimpleEnglish = "Three times a day", NormalizedToken = "three times a day", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 19, Abbreviation = "QID", SimpleEnglish = "Four times a day", NormalizedToken = "four times a day", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 20, Abbreviation = "QHS", SimpleEnglish = "Every night at bedtime", NormalizedToken = "at bedtime", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 21, Abbreviation = "QAM", SimpleEnglish = "Every morning", NormalizedToken = "every morning", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 22, Abbreviation = "QPM", SimpleEnglish = "Every evening", NormalizedToken = "every evening", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 23, Abbreviation = "Q4H", SimpleEnglish = "Every 4 hours", NormalizedToken = "every 4 hours", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 24, Abbreviation = "Q6H", SimpleEnglish = "Every 6 hours", NormalizedToken = "every 6 hours", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 25, Abbreviation = "Q8H", SimpleEnglish = "Every 8 hours", NormalizedToken = "every 8 hours", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 26, Abbreviation = "Q12H", SimpleEnglish = "Every 12 hours", NormalizedToken = "every 12 hours", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 27, Abbreviation = "QOD", SimpleEnglish = "Every other day", NormalizedToken = "every other day", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 28, Abbreviation = "PRN", SimpleEnglish = "As needed", NormalizedToken = "as needed", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 29, Abbreviation = "STAT", SimpleEnglish = "Immediately", NormalizedToken = "immediately", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 30, Abbreviation = "TAB", SimpleEnglish = "Tablet", NormalizedToken = "tablet", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 31, Abbreviation = "CAP", SimpleEnglish = "Capsule", NormalizedToken = "capsule", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 32, Abbreviation = "GTT / GTTS", SimpleEnglish = "Drop / Drops", NormalizedToken = "drops", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 33, Abbreviation = "LIQ", SimpleEnglish = "Liquid", NormalizedToken = "liquid", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 34, Abbreviation = "SOLN", SimpleEnglish = "Solution", NormalizedToken = "solution", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 35, Abbreviation = "SUSP", SimpleEnglish = "Suspension", NormalizedToken = "suspension", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 36, Abbreviation = "UNG / OINT", SimpleEnglish = "Ointment", NormalizedToken = "ointment", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 37, Abbreviation = "CRM", SimpleEnglish = "Cream", NormalizedToken = "cream", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 38, Abbreviation = "SUPP", SimpleEnglish = "Suppository", NormalizedToken = "suppository", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 39, Abbreviation = "Qty_ORD", SimpleEnglish = "Quantity Ordered", NormalizedToken = "qty ord", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 40, Abbreviation = "Qty_DSP", SimpleEnglish = "Quantity Dispensed", NormalizedToken = "qty dsp", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 41, Abbreviation = "Qty_AUTH", SimpleEnglish = "Quantity Authorized", NormalizedToken = "qty auth", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 42, Abbreviation = "AC", SimpleEnglish = "Before meals", NormalizedToken = "before meals", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 43, Abbreviation = "PC", SimpleEnglish = "After meals", NormalizedToken = "after meals", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 44, Abbreviation = "C", SimpleEnglish = "With", NormalizedToken = "with", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 45, Abbreviation = "S", SimpleEnglish = "Without", NormalizedToken = "without", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new ErxSigMapping { Id = 46, Abbreviation = "UD / AAA", SimpleEnglish = "As directed / Apply to affected area", NormalizedToken = "as directed", Category = "General", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });

            //modelBuilder.Entity<Pharmacy>().Ignore(p => p.AddressLine1);
            //modelBuilder.Entity<Pharmacy>().Ignore(p => p.AddressLine2);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    var createdProp = entry.Entity.GetType().GetProperty("CreatedDate");
                    if (createdProp != null)
                    {
                        createdProp.SetValue(entry.Entity, DateTime.UtcNow);
                    }

                    var modifiedProp = entry.Entity.GetType().GetProperty("ModifiedDate");
                    if (modifiedProp != null)
                    {
                        modifiedProp.SetValue(entry.Entity, null);
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    var modifiedProp = entry.Entity.GetType().GetProperty("ModifiedDate");
                    if (modifiedProp != null)
                    {
                        modifiedProp.SetValue(entry.Entity, DateTime.UtcNow);
                    }

                    var createdProp = entry.Entity.GetType().GetProperty("CreatedDate");
                    if (createdProp != null)
                    {
                        entry.Property("CreatedDate").IsModified = false;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}