using System.Linq;
using System.Threading.Tasks;
using FSH.Starter.Api.Data;
using FSH.Starter.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly ILlmService _llm;
    private readonly IQuotaService _quota;

    public ChatService(AppDbContext db, ILlmService llm, IQuotaService quota)
    {
        _db = db;
        _llm = llm;
        _quota = quota;
    }

    public async Task<string> ProcessMessageAsync(WhatsAppMessageDto message)
    {
        await _quota.AssertCanConsumeAsync(message.TenantId);

        var faq = await _db.Faqs
            .Where(f => f.TenantId == message.TenantId && f.Question.ToLower() == message.Body.ToLower())
            .FirstOrDefaultAsync();

        string answer;
        if (faq != null)
        {
            answer = faq.Answer;
        }
        else
        {
            var faqs = await _db.Faqs
                .Where(f => f.TenantId == message.TenantId)
                .Select(f => $"Q: {f.Question}\nA: {f.Answer}")
                .ToListAsync();

            var context = string.Join("\n\n", faqs);
            answer = await _llm.AnswerAsync(message.Body, context);
        }

        _db.ChatHistories.Add(new Entities.ChatHistory
        {
            TenantId = message.TenantId,
            UserMessage = message.Body,
            BotResponse = answer
        });
        await _db.SaveChangesAsync();

        await _quota.ConsumeAsync(message.TenantId);
        return answer;
    }
}
