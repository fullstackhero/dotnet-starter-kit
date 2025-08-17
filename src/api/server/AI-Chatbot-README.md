# AI WhatsApp Chatbot (PostgreSQL + Twilio + OpenAI)

- Entities: Tenant, Faq, ChatHistory, Subscription
- DbContext: AppDbContext (uses Npgsql when ConnectionStrings:ChatbotDb is set; SQLite fallback)
- Services: ChatService, OpenAiLlmService, QuotaService, TwilioWhatsAppService/WhatsAppStubService
- Controllers: /api/tenants, /api/faqs, /api/subscriptions, /api/webhook/whatsapp, /api/webhook/twilio

## Configure
appsettings.Development.json:
- ConnectionStrings.ChatbotDb: "Host=localhost;Port=5432;Database=chatbot;Username=postgres;Password=postgres"
- OpenAI.ApiKey: "<key>"
- WhatsApp.Provider: "Twilio"
- WhatsApp.FromNumber: "+14155551234"
- WhatsApp.AccountSid/AuthToken: from Twilio

## Test
1) POST /api/tenants
2) POST /api/subscriptions
3) POST /api/faqs
4) POST /api/webhook/whatsapp  (JSON) or set Twilio webhook to /api/webhook/twilio
