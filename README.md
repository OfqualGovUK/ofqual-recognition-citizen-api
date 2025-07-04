# Ofqual Register of Recognised Qualifications Citizen API

[![Build Status](https://ofqual.visualstudio.com/Ofqual%20IM/_apis/build/status%2Fofqual-recognition-citizen-api?branchName=main)](https://ofqual.visualstudio.com/Ofqual%20IM/_build/latest?definitionId=395&branchName=main)

The Ofqual Register of Recognised Qualifications Citizen API is used by the Register of Recognised Qualifications web application to allow the following:

- Submission of applications to be recognised
- Retrieve currently incomplete applications to allow an applicant to continue their application
- Find out if a qualification can be recognised

## Provider

[The Office of Qualifications and Examinations Regulation](https://www.gov.uk/government/organisations/ofqual)

## About this project

This project is a ASP.NET Core 8 web api utilising Docker for deployment.

The web api runs on an App service for Container apps on Azure.

# Application Configuration Guide

This document outlines how the application is configured using `appsettings.json` files. These settings help manage behaviour across different environments and scenarios, including development, production and automated testing.

## Application Settings (`appsettings.json`)

The main application settings are defined in `appsettings.json` and can be tailored per environment using files like `appsettings.Development.json` or `appsettings.Production.json`.

```json
{
  "FeatureFlag": {
    "CheckUser": true
  },
  "LogzIo": {
    "Environment": "",
    "Uri": ""
  },
  "AntiVirus": {
    "BaseUri": "",
    "AuthToken": ""
  },
  "AzureAdB2C": {
    "Instance": "",
    "Domain": "",
    "TenantId": "",
    "ClientId": "",
    "SignUpSignInPolicyId": "",
    "SignedOutCallbackPath": ""
  },
  "GovUkNotify": {
    "ApiKey": "",
    "TemplateIds": {
      "AccountCreation": "",
      "ApplicationSubmitted": "",
      "InformationFromPreEngagement": ""
    }
  },
  "ConnectionStrings": {
    "OfqualODS": "",
    "AzureBlobStorage": ""
  }
}
```

### Setting Details

- **`FeatureFlag:CheckUser`**  
  A **boolean** flag used to control whether the system attempts to retrieve application data for the current user.  
  When set to `true`, the system will check for an existing user and retrieve their latest application if available.  
  When set to `false`, the system will skip this check and treat the user as not eligible for application retrieval or creation.

- **`LogzIo:Environment`**  
  Identifies the current environment in the logs (e.g., `DEV`, `PREPROD`, `PROD`). This helps differentiate log entries across deployments.

- **`LogzIo:Uri`**  
  The endpoint for sending log data to an external logging service such as Logz.io.

- **`AntiVirus:BaseUri`**  
  The base address of the external anti-virus scanning API (e.g., `https://api.attachmentscanner.com`).

- **`AntiVirus:AuthToken`**
  The bearer token used to authenticate with the anti-virus scanning service.

- **`AzureAdB2C:Instance`**  
  The URL of the B2C service used to authenticate.

- **`AzureAdB2C:Domain`**  
  The domain we will be authenticating under.

- **`AzureAdB2C:TenantId`**  
  The unique identifier for your Azure AD B2C tenant.

- **`AzureAdB2C:ClientId`**  
  The application (client) ID registered in Azure AD B2C.

- **`AzureAdB2C:SignUpSignInPolicyId`**  
  The policy name for the typical Sign up/Sign in flow.

- **`AzureAdB2C:SignedOutCallbackPath`**  
  The callback path when signing out of Azure B2C, typically set to `/signout-callback-oidc`.

- **`GovUkNotify:ApiKey`**  
  The API key for the GovUK Notify library to function.

- **`GovUkNotify:TemplateIds`**  
  The collection of TemplateIds used for sending out GovUK Notify emails.

- **`GovUkNotify:TemplateIds:AccountCreation`**  
  The specific TemplateId used for GovUK Notify **account creation** emails.

- **`GovUkNotify:TemplateIds:ApplicationSubmitted`**  
  The specific TemplateId used for GovUK Notify **application submission confirmation** emails.

- **`GovUkNotify:TemplateIds:InformationFromPreEngagement`**  
  The specific TemplateId used for GovUK Notify **pre-engagement information** emails.

- **`ConnectionStrings:OfqualODS`**  
  Connection string for accessing the Ofqual ODS (Organisational Data Service) database.

- **`ConnectionStrings:AzureBlobStorage`**  
  The connection string for your Azure Storage account. This grants access to Blob containers and their contents.

> It’s recommended to manage environment-specific values in `appsettings.{Environment}.json` or override them via environment variables, especially in production.

## Test Settings (`appsettings.Test.json`)

Used only for **automated integration tests**, these settings configure test containers and database access. They are **not** meant for production environments.

```json
{
  "TestSettings": {
    "RegistryEndpoint": "",
    "ImagePath": "",
    "RegistryUsername": "",
    "RegistryPassword": "",
    "SqlUsername": "",
    "SqlPassword": "",
    "DatabaseName": ""
  }
}
```

### Setting Details

- **`TestSettings:RegistryEndpoint`**  
  The container registry URL where the database image is stored (e.g., Docker Hub, AWS ECR).

- **`TestSettings:ImagePath`**  
  The repository path for the test database image. Combined with the registry URL to pull the image.

- **`TestSettings:RegistryUsername`**  
  Username for accessing the container registry.

- **`TestSettings:RegistryPassword`**  
  Password or token for authenticating with the container registry.

- **`TestSettings:SqlUsername`**  
  SQL Server username for connecting to the test database container.

- **`TestSettings:SqlPassword`**  
  Corresponding password for the SQL user.

- **`TestSettings:DatabaseName`**  
   The name of the test database to be created or accessed during integration tests.

> These settings support the test environment and can be overridden in CI/CD using environment variables (e.g., `TestSettings__DatabaseName`).
