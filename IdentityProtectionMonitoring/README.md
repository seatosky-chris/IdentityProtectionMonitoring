# Identity Protection Monitoring

This is a set of Azure Functions that can be used to monitor for IDP alerts from Azure, and when new alerts are created, it will create a ticket (or update an existing ticket) in Autotask for the alert. The functions are broken down into 3 parts:

1. `CreateUpdateSubscription` - this function is a timer trigger, it will run once a day. When this function runs it will create or update a subscription in the Microsoft Graph API. This subscription watches for any new security alerts, and when any are created, the API will send a notification to the **IDPMonitor** function. See more on subscriptions/notifications: https://docs.microsoft.com/en-us/graph/webhooks
2. `IDPMonitor` - this is the primary function, it is an HTTP trigger. The Microsoft Graph API subscription is set to send notifications to this function. When this function is triggered, it verifies the notification, sends the data off to the **ProcessNotifications** function, and sends back a 202 success response. This function does not handle the notification directly because Microsoft requires you to respond to the notification within 3 seconds, and processing of it can take longer than that.
3. `ProcessNotifications` - the notification is sent to this function to be handled. The function will use the alert's ID to query the Graph API for more info. It gets extra info on the alert, risk detection, and risky user info from Azure. It will then search for any existing tickets in Autotask that might be related. If an existing ticket exists and is open, it will add a note to the ticket with the alert's details; if a ticket does not exist, it will create a new one. It will currently only create tickets for alerts that are part of the risk detection system, if any other alerts are spawned it can send an email so you can determine if these should be handled as well.


This script is design to be ran directly from a customers Azure tenant and it cannot be ran from a partner account. Additionally, it does not connect directly to an Autotask API account, it is setup to use my [AutotaskClientAPI](https://github.com/seatosky-chris/AutotaskClientAPI) azure function which sandboxes commands to that specific client. 

# Configuration: 
- Ensure the AutotaskClientAPI is setup and you have an API key for this organization. In local.settings.json, fill in the 3 Autotask related fields with the AutotaskAPIClient URL, API Key, and the Org ID of this organization in Autotask.
- If you want emails for unhandled alert types, ensure the AutomationMailer is setup and you have an API key for this organization. In local.settings.json, fill in the 6 Email related fields with the AutomationMailer URL, API Key, info on what email to send these from, and who to send the emails to.
- In local.settings.json, fill in **Client_State_Secret** and **ProcessNotifications_SecretKey** with random encryption keys. Client_State_Secret will be used by the Graph API, ProcessNotifications_SecretKey is used by the **ProcessNotifications** function to verify that any received requests were send by the IDPMonitor function. These keys should be kept secure.
- Next we need to create an Azure App for the function to authenticate with. See the separate instructions below.
- Once you have a secure app setup and have configure all of the local.settings.json variables (apart from `Notification_Url`), you can publish the function to Azure. This works best using Visual Studio.
- Once published, find the new function in Azure and navigate to the IDPMonitor function, copy the full function url (including the function key) and add this into local.settings.json, the `Notification_Url` variable. Re-publish the app and ensure it updates the settings.
- The functions are now setup and it should start running on its own within 24 hours (when the subscription get setup). To speed this up, you can manually run the `CreateUpdateSubscription` function.

# Configure an application for auth:
- Login to the organizations Azure portal and navigate to Azure **Active Directory > App Registrations**.
- Create a **"New registration"**, name it **"IDP-Monitoring"** or something similar, the rest can be left at the defaults (Single tenant, no redirect URI). 
- Under the new app's overview page, copy the **Application ID** and **Tenant ID** into local.settings.json, the `Config.IDP_Function_AppID` and `Config.IDP_Function_TenantID` variables respectively. 
- Navigate to **Manage > Certificates & Secrets**, select **New client secret**. Create a new secret that expires in 12 or 24 months. Copy the client secret **value** before you leave this page, it is never shown again so make sure you copy it now. The **secret value** can be copied into local.settings.json, the `Config.IDP_Function_Secret` variable. 
- Select **Manage > API Permissions** > **Add a permission** > **Microsoft Graph**. Select **Application Permissions**, add the following permissions:
    - SecurityEvents.Read.All
    - SecurityEvents.ReadWrite.All
    - SecurityAlert.Read.All
    - IdentityRiskEvent.Read.All
    - IdentityRiskyUser.Read.All
- Once the permissions have been added, select **Grant admin consent for Norland Limited**.
- The app is setup and can now be used by the function for authentication.