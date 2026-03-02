using FinTrack.AI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>Send a message to the AI assistant and get a response.</summary>
    [HttpPost("send")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message cannot be empty.");

        var result = await _chatService.ChatAsync(request.Message);
        return Ok(new ChatResponse
        {
            Reply = result.Reply,
            DataChanged = result.DataChanged
        });
    }

    /// <summary>Reset the chat history and start a new conversation.</summary>
    [HttpPost("reset")]
    public IActionResult ResetChat()
    {
        _chatService.ResetChat();
        return Ok(new { message = "Chat history cleared." });
    }
}

/// <summary>Request body for chat endpoint.</summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>Response body from chat endpoint.</summary>
public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;

    /// <summary>True if the AI added, updated, or deleted any assets during this turn. UI should refresh data when true.</summary>
    public bool DataChanged { get; set; }
}
