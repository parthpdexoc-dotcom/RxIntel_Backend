namespace Rxintel.Generic.Enums
{
    public static class CustomMessage
    {
        #region Login
        public const string PasswordMismatch ="Password and Confirm Password must match";
        public const string InvalidUser ="Invalid UserName or Password";
        public const string Registersuccess ="Register Successfully";
        public const string Loginsuccess ="Login Successfully";
        public const string UserAlreadyExists ="An account with this email already exists. Please log in.";
        public const string InvalidEmployeeUser = "Invalid email or password.";
        public const string EmployeeAccountInactive = "This Account is Inactive.";
        public const string SessionRefreshFailed = "Session could not be refreshed. Please sign in again.";
        public const string EmployeeEmailAlreadyExists = "An employee account with this email already exists.";
        public const string PharmacyDeaAlreadyExists = "A pharmacy with this DEA already exists.";
        public const string PharmacySignupEmailMismatch = "Pharmacy email and pharmacy user email must match.";
        public const string PharmacyNotFoundForSignup = "No pharmacy found for the given DEA and Pharmacy name.";
        public const string PharmacyDeleteBlockedByWholesalerLink =
            "This pharmacy cannot be deleted because it is linked to a pharmacy wholesaler.";
        public const string EmployeeRegistrationFieldsRequired = "Required registration fields are missing.";
        public const string TermsAcceptanceRequired = "You must accept the Terms and Conditions to register.";
        public const string TermsNotConfigured = "Terms and Conditions are not available. Registration cannot be completed at this time.";
        public const string TermsVersionMismatch = "The Terms and Conditions have changed. Please review and accept the current version.";
        public const string TermsUpdateForbidden = "You are not authorized to update Terms and Conditions.";
        public const string EmailNotificationSmtpFailed = "Email could not be delivered. Verify Admin table credentials and SMTP settings.";
        #endregion

        #region Common Dynamic Messages
        public static string GetSaveMessage(string entityName)=> $"{entityName} data saved successfully";
        public static string GetUpdateMessage(string entityName)=> $"{entityName} data updated successfully";
        public static string GetDeleteSuccessMessage(string entityName)=> $"{entityName} data deleted successfully";
        public static string GetDeleteFailedMessage(string entityName)=> $"{entityName} delete failed";
        public static string GetInvalidMessage(string entityName)=> $"Invalid {entityName} data";
        public static string GetCsvUploadSuccessMessage(string entityName)=> $"{entityName} CSV file uploaded successfully";
        #endregion

        #region Common Mapping Messages
        public static string ParentRequiredMessage(string parentName)=> $"{parentName} is required";
        public static string ChildRequiredMessage(string childName)=> $"At least one valid {childName} is required";
        public static string GetMappingSavedMessage(string mappingName)=> $"{mappingName} mappings saved successfully";
        public const string InsuranceGroupChildInsuranceRequiredMessage = "Select an insurance name";
        public const string InsuranceGroupDatesRequiredMessage = "Start date is required.";
        public const string InsuranceGroupEndDateBeforeStartMessage = "End date must be on or after start date.";
        public const string InsuranceGroupAlreadyLinkedMessage = "This insurance is already linked.";
        public const string InsuranceGroupDateOverlapMessage =
            "This date range overlaps an existing range for this insurance. Choose different dates.";
        public const string InsuranceGroupBinNumberRequiredMessage = "BIN Number is required";
        public const string InsuranceGroupBinNumberInvalidMessage = "BIN Number must contain exactly 6 digits.";
        public const string InsuranceDeleteBlockedByInsuranceGroupLink =
            "This insurance cannot be deleted because it is linked to an insurance group.";
        public const string InsuranceDeleteBlockedByInsuranceGroupChild =
            "This insurance cannot be deleted because it is linked as a child insurance in an insurance group.";
        #endregion

        #region FieldMapping
        public const string FieldMappingCsvFileRequired = "CSV file is required.";
        public const string FieldMappingPharmacyIdRequired = "PharmacyId is required.";
        public const string FieldMappingWholesalerIdRequired = "WholesalerId is required.";
        #endregion

        #region Purchase
        public const string BillingPurchaseCsvFileRequired = "Please upload a CSV or Excel (.xlsx, .xls, .xlsm) file.";
        public const string PurchaseWholesalerFieldMappingsNotFound =
            "No field mappings found for this wholesaler. Please configure Wholesaler Field Mapping first.";
        public const string PurchaseCsvNoRowsFound =
            "No purchase rows found in CSV. Check that CSV headers match saved mappings.";
        public const string CsvUploadValidationErrors =
            "The uploaded CSV file contains validation errors and cannot be uploaded.";
        public static string CsvUploadPartialSuccess(int savedRowCount, int failedRowCount) =>
            $"{savedRowCount} row(s) uploaded successfully. {failedRowCount} row(s) were skipped because of validation errors.";
        public static string RequiredMappedColumnNotFound(string columnName) =>
            $"Required mapped column '{columnName}' was not found.";
        #endregion

        #region Billing
        public const string BillingPharmacySoftwareNotConfigured =
            "Selected pharmacy has no pharmacy software configured. Please configure it in Pharmacy first.";
        public const string BillingCsvSelectPharmacyFirst =
            "Please select Pharmacy before uploading CSV.";
        public const string BillingCsvPrimaryBinRequired =
            "Primary Insurance BIN is required in CSV.";
        public const string BillingCsvPrimaryBinInvalidFormat =
            "Primary Insurance BIN must be exactly 6 digits.";
        public static string BillingCsvPrimaryBinNotRecognized(string bin) =>
            $"Primary Insurance BIN '{bin}' does not match any insurance in the system.";
        public const string BillingCsvSecondaryBinRequired =
            "Secondary Insurance BIN is required when Secondary Insurance Paid Amount is greater than zero.";
        public const string BillingCsvSecondaryBinInvalidFormat =
            "Secondary Insurance BIN must be exactly 6 digits.";
        public static string BillingCsvSecondaryBinNotRecognized(string bin) =>
            $"Secondary Insurance BIN '{bin}' does not match any insurance in the system.";
        public const string BillingFieldMappingsNotFound =
            "No field mappings found for this pharmacy software. Please configure Billing Field Mapping first.";
        public const string BillingCsvNoRowsFound =
            "No billing rows found in CSV. Check that CSV headers match saved mappings.";
        #endregion

        #region Medicine
        public const string MedicineDeleteBlockedByNdcGroupLink =
            "This medicine cannot be deleted because it is linked to an NDC group.";
        public const string MedicineDeleteBlockedByNdcGroupChild =
            "This medicine cannot be deleted because it is linked as a child medicine in an NDC group.";
        #endregion

    }
}