using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace FinTrack.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin that describes what FinTrack AI can do.
/// The LLM calls this when the user asks "what can you do?", "help", etc.
/// </summary>
public class HelpPlugin
{
    [KernelFunction("get_capabilities")]
    [Description("Returns a description of what FinTrack AI can do. Call this when the user asks 'what can you do','who are you', 'help', 'what are your capabilities', 'describe yourself', or similar questions.")]
    public string GetCapabilities()
    {
        return """
            I'm **FinTrack AI** — your personal finance assistant. Here's what I can do:

            **?? Portfolio Overview**
            - Show your total net worth with live market prices
            - Break down assets by Gold, Silver, and Fixed Deposits
            - Show profit/loss with percentage for each asset

            **?? Gold Management**
            - Add gold purchases (weight in grams + purchase rate per gram)
            - Track 22K gold with sovereign conversion (8g = 1 sovereign)
            - View current gold value at live market rates

            **?? Silver Management**
            - Add silver purchases (weight in grams + purchase rate per gram)
            - Track silver holdings with live market pricing

            **?? Fixed Deposits**
            - Add FDs with principal, interest rate, tenure, and bank name
            - Track maturity with compound interest calculation
            - Bank-wise FD breakdown

            **?? Edit & Delete**
            - Update any asset (quantity, rate, date, bank, etc.)
            - Delete individual assets or all assets of a type
            - Example: "remove all gold" or "delete the SBI FD"

            **?? Live Prices**
            - Check current gold (22K) and silver prices per gram
            - See daily price change percentage

            **?? Try asking:**
            - "Add 8 grams of gold purchased at ?7,500/g on Jan 15"
            - "What's my net worth?"
            - "Show my FDs"
            - "Delete the silver entry"
            - "What's today's gold price?"
            """;
    }
}
