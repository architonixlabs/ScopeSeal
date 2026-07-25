using Microsoft.AspNetCore.Hosting;

namespace ScopeSeal.Api.Tests;

public sealed class BillingPostgresWebApplicationFactory : PostgresWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ScopeSeal:Billing:Mode", "LocalTest");
        builder.UseSetting("ScopeSeal:Billing:TestModeOnly", "true");
        builder.UseSetting("ScopeSeal:Billing:Razorpay:KeyId", "rzp_test_scopeseal");
        builder.UseSetting("ScopeSeal:Billing:Razorpay:KeySecret", "test_key_secret_for_hmac_signatures");
        builder.UseSetting("ScopeSeal:Billing:Razorpay:WebhookSecret", "test_webhook_secret_for_signatures");
        builder.UseSetting("ScopeSeal:Billing:Plans:Pro:MonthlyRazorpayPlanId", "plan_test_pro_monthly");
        builder.UseSetting("ScopeSeal:Billing:Plans:Pro:AnnualRazorpayPlanId", "plan_test_pro_annual");
        builder.UseSetting("ScopeSeal:Billing:Plans:Business:MonthlyRazorpayPlanId", "plan_test_business_monthly");
        builder.UseSetting("ScopeSeal:Billing:Plans:Business:AnnualRazorpayPlanId", "plan_test_business_annual");
    }
}
