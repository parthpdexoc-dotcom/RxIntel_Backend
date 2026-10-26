using System.ComponentModel;

namespace Rxintel.Generic.Models.Constants
{
    public class ModelValidationErrorCodes : ModelErrorCodes
    {
        [Description("Request is invalid")]
        public const string INVALID_REQUEST = "INVALID_REQUEST";

        #region Terms
        [Description("Terms and Conditions content is required")]
        public const string REQUIRED_TERMS_CONTENT = "REQUIRED_TERMS_CONTENT";

        [Description("Terms and Conditions acceptance is required")]
        public const string REQUIRED_TERMS_ACCEPTANCE = "REQUIRED_TERMS_ACCEPTANCE";
        #endregion

        #region PharmacyDto
        [Description("DEA is required")]
        public const string REQUIRED_PHARMACY_DEA = "REQUIRED_PHARMACY_DEA";

        [Description("DEA number or DEA document is required")]
        public const string REQUIRED_PHARMACY_DEA_OR_DOCUMENT = "REQUIRED_PHARMACY_DEA_OR_DOCUMENT";

        [Description("State Board License number or license document is required")]
        public const string REQUIRED_PHARMACY_STATE_BOARD_LICENSE_OR_DOCUMENT = "REQUIRED_PHARMACY_STATE_BOARD_LICENSE_OR_DOCUMENT";

        [Description("Uploaded document must be PDF, JPG, PNG, or Excel")]
        public const string INVALID_PHARMACY_DOCUMENT_FORMAT = "INVALID_PHARMACY_DOCUMENT_FORMAT";

        [Description("Uploaded document cannot exceed 5 MB")]
        public const string INVALID_PHARMACY_DOCUMENT_SIZE = "INVALID_PHARMACY_DOCUMENT_SIZE";

        [Description("DEA must be 2 letters followed by 7 digits")]
        public const string INVALID_PHARMACY_DEA_FORMAT = "INVALID_PHARMACY_DEA_FORMAT";

        [Description("DEA second letter must match the first letter of Business Trade Name")]
        public const string INVALID_PHARMACY_DEA_BUSINESS_NAME_LETTER = "INVALID_PHARMACY_DEA_BUSINESS_NAME_LETTER";

        [Description("DEA check digit is invalid")]
        public const string INVALID_PHARMACY_DEA_CHECKSUM = "INVALID_PHARMACY_DEA_CHECKSUM";

        [Description("Business Legal Name is required")]
        public const string REQUIRED_PHARMACY_BUSINESS_LEGAL_NAME = "REQUIRED_PHARMACY_BUSINESS_LEGAL_NAME";

        [Description("Business Legal Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_BUSINESS_LEGAL_NAME = "MAXIMUM_LENGTH_PHARMACY_BUSINESS_LEGAL_NAME";

        [Description("Business Legal Name must contain only letters, numbers, spaces, and . & ' ( ) - characters")]
        public const string INVALID_PHARMACY_BUSINESS_LEGAL_NAME_FORMAT = "INVALID_PHARMACY_BUSINESS_LEGAL_NAME_FORMAT";

        [Description("Business Trade Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_BUSINESS_TRADE_NAME = "MAXIMUM_LENGTH_PHARMACY_BUSINESS_TRADE_NAME";

        [Description("DEA Expiration Date is required")]
        public const string REQUIRED_PHARMACY_DEA_EXPIRATION_DATE = "REQUIRED_PHARMACY_DEA_EXPIRATION_DATE";

        [Description("State Board License Number is required")]
        public const string REQUIRED_PHARMACY_STATE_BOARD_LICENSE = "REQUIRED_PHARMACY_STATE_BOARD_LICENSE";

        [Description("State Board License Number cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_STATE_BOARD_LICENSE = "MAXIMUM_LENGTH_PHARMACY_STATE_BOARD_LICENSE";

        [Description("License number must contain both letters and numbers and may contain hyphens. Length must be between 5 and 20 characters.")]
        public const string INVALID_PHARMACY_STATE_BOARD_LICENSE_FORMAT = "INVALID_PHARMACY_STATE_BOARD_LICENSE_FORMAT";

        [Description("BOP License Expiration Date is required")]
        public const string REQUIRED_PHARMACY_BOP_LICENSE_EXPIRATION_DATE = "REQUIRED_PHARMACY_BOP_LICENSE_EXPIRATION_DATE";

        [Description("Global Location Number must be exactly 13 digits")]
        public const string INVALID_PHARMACY_GLN_FORMAT = "INVALID_PHARMACY_GLN_FORMAT";

        [Description("Pharmacy Address is required")]
        public const string REQUIRED_PHARMACY_ADDRESS = "REQUIRED_PHARMACY_ADDRESS";

        [Description("Pharmacy Address cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_ADDRESS = "MAXIMUM_LENGTH_PHARMACY_ADDRESS";

        [Description("Pharmacy Address must contain only letters, numbers, spaces, and , . / # - characters")]
        public const string INVALID_PHARMACY_ADDRESS_FORMAT = "INVALID_PHARMACY_ADDRESS_FORMAT";

        [Description("Street Address cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_STREET_ADDRESS = "MAXIMUM_LENGTH_PHARMACY_STREET_ADDRESS";

        [Description("Street Address must contain only letters, numbers, spaces, and , . / # - characters")]
        public const string INVALID_PHARMACY_STREET_ADDRESS_FORMAT = "INVALID_PHARMACY_STREET_ADDRESS_FORMAT";

        [Description("City is required")]
        public const string REQUIRED_PHARMACY_CITY = "REQUIRED_PHARMACY_CITY";

        [Description("State is required")]
        public const string REQUIRED_PHARMACY_STATE = "REQUIRED_PHARMACY_STATE";

        [Description("Zip Code is required")]
        public const string REQUIRED_PHARMACY_ZIP = "REQUIRED_PHARMACY_ZIP";

        [Description("Phone is required")]
        public const string REQUIRED_PHARMACY_PHONE = "REQUIRED_PHARMACY_PHONE";

        [Description("Fax must be exactly 10 digits")]
        public const string INVALID_PHARMACY_FAX_FORMAT = "INVALID_PHARMACY_FAX_FORMAT";

        [Description("Email is required")]
        public const string REQUIRED_PHARMACY_EMAIL = "REQUIRED_PHARMACY_EMAIL";

        [Description("Pharmacy Software System cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_SOFTWARE_SYSTEM = "MAXIMUM_LENGTH_PHARMACY_SOFTWARE_SYSTEM";

        [Description("Pharmacy Software System must contain only letters, numbers, spaces, and . / - characters")]
        public const string INVALID_PHARMACY_SOFTWARE_SYSTEM_FORMAT = "INVALID_PHARMACY_SOFTWARE_SYSTEM_FORMAT";

        [Description("Pharmacy Software Phone must be exactly 10 digits")]
        public const string INVALID_PHARMACY_SOFTWARE_PHONE_FORMAT = "INVALID_PHARMACY_SOFTWARE_PHONE_FORMAT";

        [Description("Pharmacy Software Email must be a valid format")]
        public const string INVALID_PHARMACY_SOFTWARE_EMAIL_FORMAT = "INVALID_PHARMACY_SOFTWARE_EMAIL_FORMAT";

        [Description("Pharmacy Software Email cannot exceed 255 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_SOFTWARE_EMAIL = "MAXIMUM_LENGTH_PHARMACY_SOFTWARE_EMAIL";

        [Description("Pharmacy Name is required")]
        public const string REQUIRED_PHARMACY_NAME = "REQUIRED_PHARMACY_NAME";

        [Description("Pharmacy Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_NAME = "MAXIMUM_LENGTH_PHARMACY_NAME";

        [Description("Pharmacy Name must contain only letters, numbers, spaces, and . & ' ( ) - characters")]
        public const string INVALID_PHARMACY_NAME_FORMAT = "INVALID_PHARMACY_NAME_FORMAT";

        [Description("NPI must be exactly 10 digits")]
        public const string INVALID_PHARMACY_NPI_FORMAT = "INVALID_PHARMACY_NPI_FORMAT";

        [Description("City must contain only letters, spaces, apostrophes, and hyphens")]
        public const string INVALID_PHARMACY_CITY_FORMAT = "INVALID_PHARMACY_CITY_FORMAT";

        [Description("City cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_CITY = "MAXIMUM_LENGTH_PHARMACY_CITY";

        [Description("Zip Code must be exactly 5 digits")]
        public const string INVALID_PHARMACY_ZIP_FORMAT = "INVALID_PHARMACY_ZIP_FORMAT";

        [Description("Phone Number must be exactly 10 digits")]
        public const string INVALID_PHARMACY_PHONE_FORMAT = "INVALID_PHARMACY_PHONE_FORMAT";

        [Description("Email must be a valid format ")]
        public const string INVALID_PHARMACY_EMAIL_FORMAT = "INVALID_PHARMACY_EMAIL_FORMAT";

        [Description("Email cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_EMAIL = "MAXIMUM_LENGTH_PHARMACY_EMAIL";
        #endregion

        #region PharmacyUserDto
        [Description("User Id is required")]
        public const string REQUIRED_PHARMACY_USER_ID = "REQUIRED_PHARMACY_USER_ID";

        [Description("User Id cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_USER_ID = "MAXIMUM_LENGTH_PHARMACY_USER_ID";

        [Description("User Id must contain only letters, numbers, and . _ - with no spaces or special characters")]
        public const string INVALID_PHARMACY_USER_ID_FORMAT = "INVALID_PHARMACY_USER_ID_FORMAT";

        [Description("Role must be a valid pharmacy user role (1 = Manager, 2 = Pharmacy admin, 3 = Pharmacist, 4 = Pharmacy Tech, 5 = Admin)")]
        public const string INVALID_PHARMACY_USER_ROLE = "INVALID_PHARMACY_USER_ROLE";

        [Description("Name must contain only letters and spaces")]
        public const string INVALID_PHARMACY_USER_NAME_FORMAT = "INVALID_PHARMACY_USER_NAME_FORMAT";

        [Description("Name is required")]
        public const string REQUIRED_PHARMACY_USER_NAME = "REQUIRED_PHARMACY_USER_NAME";

        [Description("Name cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_USER_NAME = "MAXIMUM_LENGTH_PHARMACY_USER_NAME";

        [Description("Email is required")]
        public const string REQUIRED_PHARMACY_USER_EMAIL = "REQUIRED_PHARMACY_USER_EMAIL";

        [Description("Email must be a valid format")]
        public const string INVALID_PHARMACY_USER_EMAIL_FORMAT = "INVALID_PHARMACY_USER_EMAIL_FORMAT";

        [Description("Email cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_USER_EMAIL = "MAXIMUM_LENGTH_PHARMACY_USER_EMAIL";

        [Description("Password is required")]
        public const string REQUIRED_PHARMACY_USER_PASSWORD = "REQUIRED_PHARMACY_USER_PASSWORD";

        [Description("Password must be at least 8 characters and include letters, numbers, and one symbol")]
        public const string INVALID_PHARMACY_USER_PASSWORD_FORMAT = "INVALID_PHARMACY_USER_PASSWORD_FORMAT";

        [Description("Profile image must be a valid image filename (JPG, PNG, or GIF)")]
        public const string INVALID_PHARMACY_USER_PROFILE_IMAGE_FORMAT = "INVALID_PHARMACY_USER_PROFILE_IMAGE_FORMAT";

        [Description("Profile image cannot exceed 2 MB")]
        public const string INVALID_PHARMACY_USER_PROFILE_IMAGE_SIZE = "INVALID_PHARMACY_USER_PROFILE_IMAGE_SIZE";

        [Description("Confirm Password is required")]
        public const string REQUIRED_PHARMACY_USER_CONFIRM_PASSWORD = "REQUIRED_PHARMACY_USER_CONFIRM_PASSWORD";

        [Description("Password and Confirm Password must match")]
        public const string PASSWORD_MISMATCH = "PASSWORD_MISMATCH";

        [Description("Primary Contact Name is required")]
        public const string REQUIRED_PHARMACY_PRIMARY_CONTACT_NAME = "REQUIRED_PHARMACY_PRIMARY_CONTACT_NAME";

        [Description("Primary Contact Name cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_PRIMARY_CONTACT_NAME = "MAXIMUM_LENGTH_PHARMACY_PRIMARY_CONTACT_NAME";

        [Description("Primary Contact Name must contain only letters, spaces, apostrophes, and hyphens")]
        public const string INVALID_PHARMACY_PRIMARY_CONTACT_NAME_FORMAT = "INVALID_PHARMACY_PRIMARY_CONTACT_NAME_FORMAT";

        [Description("Primary Contact Title cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_PRIMARY_CONTACT_TITLE = "MAXIMUM_LENGTH_PHARMACY_PRIMARY_CONTACT_TITLE";

        [Description("Primary Contact Title must contain only letters, numbers, spaces, apostrophes, and hyphens")]
        public const string INVALID_PHARMACY_PRIMARY_CONTACT_TITLE_FORMAT = "INVALID_PHARMACY_PRIMARY_CONTACT_TITLE_FORMAT";

        [Description("Primary Contact Phone is required")]
        public const string REQUIRED_PHARMACY_PRIMARY_CONTACT_PHONE = "REQUIRED_PHARMACY_PRIMARY_CONTACT_PHONE";

        [Description("Primary Contact Phone must be exactly 10 digits")]
        public const string INVALID_PHARMACY_PRIMARY_CONTACT_PHONE_FORMAT = "INVALID_PHARMACY_PRIMARY_CONTACT_PHONE_FORMAT";

        [Description("Primary Wholesaler Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_NAME = "MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_NAME";

        [Description("Primary Wholesaler Name must contain only letters, numbers and . & ' - characters")]
        public const string INVALID_PHARMACY_PRIMARY_WHOLESALER_NAME_FORMAT = "INVALID_PHARMACY_PRIMARY_WHOLESALER_NAME_FORMAT";

        [Description("Primary Wholesaler Contact Name cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_CONTACT_NAME = "MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_CONTACT_NAME";

        [Description("Primary Wholesaler Contact Name must contain only letters, spaces, apostrophes, and hyphens")]
        public const string INVALID_PHARMACY_PRIMARY_WHOLESALER_CONTACT_NAME_FORMAT = "INVALID_PHARMACY_PRIMARY_WHOLESALER_CONTACT_NAME_FORMAT";

        [Description("Primary Wholesaler Phone must be exactly 10 digits")]
        public const string INVALID_PHARMACY_PRIMARY_WHOLESALER_PHONE_FORMAT = "INVALID_PHARMACY_PRIMARY_WHOLESALER_PHONE_FORMAT";

        [Description("Primary Wholesaler Email must be a valid format")]
        public const string INVALID_PHARMACY_PRIMARY_WHOLESALER_EMAIL_FORMAT = "INVALID_PHARMACY_PRIMARY_WHOLESALER_EMAIL_FORMAT";

        [Description("Primary Wholesaler Email cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_EMAIL = "MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_EMAIL";

        #endregion

        #region InsuranceDto
        [Description("Insurance Name is required")]
        public const string REQUIRED_INSURANCE_NAME = "REQUIRED_INSURANCE_NAME";

        [Description("Insurance Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_INSURANCE_NAME = "MAXIMUM_LENGTH_INSURANCE_NAME";

        [Description("Insurance Name must contain only letters, numbers, spaces, and . & ' ( ) - characters")]
        public const string INVALID_INSURANCE_NAME_FORMAT = "INVALID_INSURANCE_NAME_FORMAT";

        [Description("BIN Number is required")]
        public const string REQUIRED_INSURANCE_BIN_NUMBER = "REQUIRED_INSURANCE_BIN_NUMBER";

        [Description("BIN Number must contain exactly 6 digits.")]
        public const string INVALID_INSURANCE_BIN_NUMBER_FORMAT = "INVALID_INSURANCE_BIN_NUMBER_FORMAT";

        [Description("BIN Number cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_INSURANCE_BIN_NUMBER = "MAXIMUM_LENGTH_INSURANCE_BIN_NUMBER";

        [Description("Phone must be exactly 10 digits")]
        public const string INVALID_INSURANCE_PHONE_FORMAT = "INVALID_INSURANCE_PHONE_FORMAT";

        [Description("Email must be a valid format")]
        public const string INVALID_INSURANCE_EMAIL_FORMAT = "INVALID_INSURANCE_EMAIL_FORMAT";

        [Description("Email cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_INSURANCE_EMAIL = "MAXIMUM_LENGTH_INSURANCE_EMAIL";
        #endregion

        #region WholesalerDto
        [Description("Wholesaler Name is required")]
        public const string REQUIRED_WHOLESALER_NAME = "REQUIRED_WHOLESALER_NAME";

        [Description("Wholesaler Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_WHOLESALER_NAME = "MAXIMUM_LENGTH_WHOLESALER_NAME";

        [Description("Wholesaler Name must contain only letters, numbers, spaces, and . & ' ( ) - characters")]
        public const string INVALID_WHOLESALER_NAME_FORMAT = "INVALID_WHOLESALER_NAME_FORMAT";

        [Description("Address cannot exceed 500 characters")]
        public const string MAXIMUM_LENGTH_WHOLESALER_ADDRESS = "MAXIMUM_LENGTH_WHOLESALER_ADDRESS";

        [Description("Phone must be exactly 10 digits")]
        public const string INVALID_WHOLESALER_PHONE_FORMAT = "INVALID_WHOLESALER_PHONE_FORMAT";

        [Description("Email must be a valid format")]
        public const string INVALID_WHOLESALER_EMAIL_FORMAT = "INVALID_WHOLESALER_EMAIL_FORMAT";

        [Description("Email cannot exceed 150 characters")]
        public const string MAXIMUM_LENGTH_WHOLESALER_EMAIL = "MAXIMUM_LENGTH_WHOLESALER_EMAIL";
        #endregion

        #region PharmacySoftwareDto
        [Description("Software Name is required")]
        public const string REQUIRED_PHARMACY_SOFTWARE_NAME = "REQUIRED_PHARMACY_SOFTWARE_NAME";

        [Description("Software Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_PHARMACY_SOFTWARE_NAME = "MAXIMUM_LENGTH_PHARMACY_SOFTWARE_NAME";

        [Description("Software Name must contain only letters, numbers, spaces, and . / - characters")]
        public const string INVALID_PHARMACY_SOFTWARE_NAME_FORMAT = "INVALID_PHARMACY_SOFTWARE_NAME_FORMAT";
        #endregion

        #region MedicineDto
        [Description("NDC is required")]
        public const string REQUIRED_MEDICINE_NDC = "REQUIRED_MEDICINE_NDC";

        [Description("NDC cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_NDC = "MAXIMUM_LENGTH_MEDICINE_NDC";

        [Description("NDC must be a valid US format (10-11 digits or hyphenated, e.g. 67234-020-01, 54868-5900-0)")]
        public const string INVALID_MEDICINE_NDC_FORMAT = "INVALID_MEDICINE_NDC_FORMAT";

        [Description("Brand Name is required")]
        public const string REQUIRED_MEDICINE_DRUG_NAME = "REQUIRED_MEDICINE_DRUG_NAME";
        
        [Description("Brand Name cannot exceed 250 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_DRUG_NAME = "MAXIMUM_LENGTH_MEDICINE_DRUG_NAME";

        [Description("Brand Name contains invalid characters")]
        public const string INVALID_MEDICINE_DRUG_NAME_FORMAT = "INVALID_MEDICINE_DRUG_NAME_FORMAT";

        [Description("Strength must be a valid strength unit (e.g. MG, ML, MCG, MG/ML)")]
        public const string INVALID_MEDICINE_STRENGTH = "INVALID_MEDICINE_STRENGTH";

        [Description("Strength must be a numeric value or semicolon-separated numeric values (e.g. 10, 2; 8; 4.5; 7)")]
        public const string INVALID_MEDICINE_STRENGTH_VALUE_FORMAT = "INVALID_MEDICINE_STRENGTH_VALUE_FORMAT";

        [Description("Strength cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_STRENGTH = "MAXIMUM_LENGTH_MEDICINE_STRENGTH";

        [Description("Unit must be a numeric value only (e.g. 60, 2.5, 473)")]
        public const string INVALID_MEDICINE_UNIT_FORMAT = "INVALID_MEDICINE_UNIT_FORMAT";

        [Description("Unit cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_UNIT = "MAXIMUM_LENGTH_MEDICINE_UNIT";

        [Description("Package Size is required")]
        public const string REQUIRED_MEDICINE_PACKAGE_SIZE = "REQUIRED_MEDICINE_PACKAGE_SIZE";

        [Description("Package Size must be a valid format")]
        public const string INVALID_MEDICINE_PACKAGE_SIZE_FORMAT = "INVALID_MEDICINE_PACKAGE_SIZE_FORMAT";

        [Description("Package Size cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_PACKAGE_SIZE = "MAXIMUM_LENGTH_MEDICINE_PACKAGE_SIZE";

        [Description("Form must be a valid dosage form (e.g. Tablet, Capsule, Syrup, Injection)")]
        public const string INVALID_MEDICINE_FORM = "INVALID_MEDICINE_FORM";

        [Description("Product NDC cannot exceed 20 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_PRODUCT_NDC = "MAXIMUM_LENGTH_MEDICINE_PRODUCT_NDC";

        [Description("Product NDC must be in valid hyphenated format (e.g. 51662-1407, 67234-020, 0904-7035)")]
        public const string INVALID_MEDICINE_PRODUCT_NDC_FORMAT = "INVALID_MEDICINE_PRODUCT_NDC_FORMAT";

        [Description("Generic cannot exceed 500 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_GENERIC = "MAXIMUM_LENGTH_MEDICINE_GENERIC";

        [Description("Generic contains invalid characters. Only letters, numbers, spaces, and . , ( ) / % ' + - are allowed.")]
        public const string INVALID_MEDICINE_GENERIC_FORMAT = "INVALID_MEDICINE_GENERIC_FORMAT";

        [Description("Route cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_ROUTE = "MAXIMUM_LENGTH_MEDICINE_ROUTE";

        [Description("Route contains invalid characters. Only letters, spaces, and ( ) ; are allowed.")]
        public const string INVALID_MEDICINE_ROUTE_FORMAT = "INVALID_MEDICINE_ROUTE_FORMAT";

        [Description("Labeler cannot exceed 250 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_LABELER = "MAXIMUM_LENGTH_MEDICINE_LABELER";

        [Description("Labeler contains invalid characters. Only letters, numbers, spaces, and . , - & ( ) ' _ / are allowed.")]
        public const string INVALID_MEDICINE_LABELER_FORMAT = "INVALID_MEDICINE_LABELER_FORMAT";

        [Description("Suffix cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_MEDICINE_SUFFIX = "MAXIMUM_LENGTH_MEDICINE_SUFFIX";

        [Description("Suffix contains invalid characters. Only letters, numbers, spaces, and . , ( ) / % - are allowed.")]
        public const string INVALID_MEDICINE_SUFFIX_FORMAT = "INVALID_MEDICINE_SUFFIX_FORMAT";
        #endregion

        #region PurchaseDto
        [Description("Pharmacy is required")]
        public const string INVALID_PURCHASE_PHARMACY_ID = "INVALID_PURCHASE_PHARMACY_ID";

        [Description("Wholesaler is required")]
        public const string INVALID_PURCHASE_WHOLESALER_ID = "INVALID_PURCHASE_WHOLESALER_ID";

        [Description("NDC is required")]
        public const string REQUIRED_PURCHASE_NDC = "REQUIRED_PURCHASE_NDC";

        [Description("NDC cannot exceed 20 characters")]
        public const string MAXIMUM_LENGTH_PURCHASE_NDC = "MAXIMUM_LENGTH_PURCHASE_NDC";

        [Description("NDC must be 10-11 digits or a valid hyphenated US format (e.g. 0002-8215-01, 59762-3720-1)")]
        public const string INVALID_PURCHASE_NDC_FORMAT = "INVALID_PURCHASE_NDC_FORMAT";

        [Description("Quantity Purchased must be greater than zero")]
        public const string INVALID_PURCHASE_QUANTITY = "INVALID_PURCHASE_QUANTITY";

        [Description("Package Size must be a valid format")]
        public const string INVALID_PURCHASE_PACKAGE_SIZE_FORMAT = "INVALID_PURCHASE_PACKAGE_SIZE_FORMAT";

        [Description("Package Size cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_PURCHASE_PACKAGE_SIZE = "MAXIMUM_LENGTH_PURCHASE_PACKAGE_SIZE";

        [Description("Invoice Date cannot be in the future or more than 10 years in the past")]
        public const string INVALID_PURCHASE_INVOICE_DATE = "INVALID_PURCHASE_INVOICE_DATE";

        [Description("Invoice Number must contain only letters, numbers, hyphens, and slashes")]
        public const string INVALID_PURCHASE_INVOICE_NUMBER_FORMAT = "INVALID_PURCHASE_INVOICE_NUMBER_FORMAT";

        [Description("Invoice Number cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_PURCHASE_INVOICE_NUMBER = "MAXIMUM_LENGTH_PURCHASE_INVOICE_NUMBER";
        #endregion

        #region BillingDto
        [Description("Pharmacy is required")]
        public const string INVALID_BILLING_PHARMACY_ID = "INVALID_BILLING_PHARMACY_ID";

        [Description("Insurance is required")]
        public const string INVALID_BILLING_INSURANCE_ID = "INVALID_BILLING_INSURANCE_ID";

        [Description("Secondary Insurance is required when secondary paid amount is greater than zero")]
        public const string REQUIRED_BILLING_SECONDARY_INSURANCE_ID = "REQUIRED_BILLING_SECONDARY_INSURANCE_ID";

        [Description("Secondary Insurance is invalid")]
        public const string INVALID_BILLING_SECONDARY_INSURANCE_ID = "INVALID_BILLING_SECONDARY_INSURANCE_ID";

        [Description("NDC is required")]
        public const string REQUIRED_BILLING_NDC = "REQUIRED_BILLING_NDC";

        [Description("NDC cannot exceed 20 characters")]
        public const string MAXIMUM_LENGTH_BILLING_NDC = "MAXIMUM_LENGTH_BILLING_NDC";

        [Description("NDC must be 10-11 digits or a valid hyphenated US format (e.g. 0002-8215-01, 12345678901)")]
        public const string INVALID_BILLING_NDC_FORMAT = "INVALID_BILLING_NDC_FORMAT";

        [Description("Quantity Billed must be greater than zero")]
        public const string INVALID_BILLING_QUANTITY = "INVALID_BILLING_QUANTITY";

        [Description("RX Number must contain only letters, numbers, and hyphens")]
        public const string INVALID_BILLING_RXNO_FORMAT = "INVALID_BILLING_RXNO_FORMAT";

        [Description("RX Number cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_BILLING_RXNO = "MAXIMUM_LENGTH_BILLING_RXNO";

        [Description("Bill Date cannot be in the future or more than 10 years in the past")]
        public const string INVALID_BILLING_BILL_DATE = "INVALID_BILLING_BILL_DATE";

        [Description("Drug Name is required")]
        public const string REQUIRED_BILLING_DRUG_NAME = "REQUIRED_BILLING_DRUG_NAME";

        [Description("Drug Name contains invalid characters")]
        public const string INVALID_BILLING_DRUG_NAME_FORMAT = "INVALID_BILLING_DRUG_NAME_FORMAT";

        [Description("Drug Name cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_BILLING_DRUG_NAME = "MAXIMUM_LENGTH_BILLING_DRUG_NAME";

        [Description("Drug Form must be a valid dosage form (e.g. Tablet, Capsule, Syrup, Injection)")]
        public const string INVALID_BILLING_DRUG_FORM_FORMAT = "INVALID_BILLING_DRUG_FORM_FORMAT";

        [Description("Drug Form cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_BILLING_DRUG_FORM = "MAXIMUM_LENGTH_BILLING_DRUG_FORM";

        [Description("Package Size must be a valid format")]
        public const string INVALID_BILLING_PACKAGE_SIZE_FORMAT = "INVALID_BILLING_PACKAGE_SIZE_FORMAT";

        [Description("Package Size cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_BILLING_PACKAGE_SIZE = "MAXIMUM_LENGTH_BILLING_PACKAGE_SIZE";

        [Description("Drug Therapy must contain only letters, spaces, hyphens, and apostrophes")]
        public const string INVALID_BILLING_DRUG_THERAPY_FORMAT = "INVALID_BILLING_DRUG_THERAPY_FORMAT";

        [Description("Drug Therapy cannot exceed 200 characters")]
        public const string MAXIMUM_LENGTH_BILLING_DRUG_THERAPY = "MAXIMUM_LENGTH_BILLING_DRUG_THERAPY";

        [Description("Days must be between 1 and 365")]
        public const string INVALID_BILLING_DAYS = "INVALID_BILLING_DAYS";

        [Description("Primary Insurance Paid Amount must be zero or greater with at most 2 decimal places")]
        public const string INVALID_BILLING_PRIMARY_PAID_AMOUNT = "INVALID_BILLING_PRIMARY_PAID_AMOUNT";

        [Description("Primary Insurance BIN is required")]
        public const string REQUIRED_BILLING_PRIMARY_BIN = "REQUIRED_BILLING_PRIMARY_BIN";

        [Description("Primary Insurance BIN must be exactly 6 digits")]
        public const string INVALID_BILLING_PRIMARY_BIN_FORMAT = "INVALID_BILLING_PRIMARY_BIN_FORMAT";

        [Description("Primary Insurance BIN cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_BILLING_PRIMARY_BIN = "MAXIMUM_LENGTH_BILLING_PRIMARY_BIN";

        [Description("Secondary Insurance Paid Amount must be zero or greater with at most 2 decimal places")]
        public const string INVALID_BILLING_SECONDARY_PAID_AMOUNT = "INVALID_BILLING_SECONDARY_PAID_AMOUNT";

        [Description("Secondary Insurance name is required when secondary paid amount is greater than zero")]
        public const string REQUIRED_BILLING_SECONDARY_INSURANCE = "REQUIRED_BILLING_SECONDARY_INSURANCE";

        [Description("Secondary Insurance name contains invalid characters")]
        public const string INVALID_BILLING_SECONDARY_INSURANCE_FORMAT = "INVALID_BILLING_SECONDARY_INSURANCE_FORMAT";

        [Description("Secondary Insurance cannot exceed 100 characters")]
        public const string MAXIMUM_LENGTH_BILLING_SECONDARY_INSURANCE = "MAXIMUM_LENGTH_BILLING_SECONDARY_INSURANCE";

        [Description("Secondary Insurance BIN is required")]
        public const string REQUIRED_BILLING_SECONDARY_BIN = "REQUIRED_BILLING_SECONDARY_BIN";

        [Description("Secondary Insurance BIN must be exactly 6 digits")]
        public const string INVALID_BILLING_SECONDARY_BIN_FORMAT = "INVALID_BILLING_SECONDARY_BIN_FORMAT";

        [Description("Secondary Insurance BIN cannot exceed 50 characters")]
        public const string MAXIMUM_LENGTH_BILLING_SECONDARY_BIN = "MAXIMUM_LENGTH_BILLING_SECONDARY_BIN";

        [Description("Total Insurance Paid Amount must be zero or greater with at most 2 decimal places")]
        public const string INVALID_BILLING_TOTAL_PAID_AMOUNT = "INVALID_BILLING_TOTAL_PAID_AMOUNT";

        [Description("Total Insurance Paid Amount must equal Primary plus Secondary paid amounts")]
        public const string INVALID_BILLING_TOTAL_PAID_SUM = "INVALID_BILLING_TOTAL_PAID_SUM";
        #endregion
    }
}
