using StardewValley.Menus;
using StardewValley;

public class DialogueBoxWithActions : DialogueBox
{
    private readonly List<Response> responses;
    private readonly System.Action<string> onChoice;
    private readonly NPC speaker;

    public DialogueBoxWithActions(string dialogueText, List<Response> responses, System.Action<string> onChoice, NPC speaker = null)
        : base(dialogueText, responses.ToArray())
    {
        this.responses = responses;
        this.onChoice = onChoice;
        this.speaker = speaker;

        if (speaker != null)
            Game1.currentSpeaker = speaker;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.selectedResponse != null)
        {
            int index = this.selectedResponse;
            if (index >= 0 && index < responses.Count)
            {
                string key = responses[index].responseKey;
                onChoice?.Invoke(key);
                Game1.exitActiveMenu();
            }
        }
    }
}