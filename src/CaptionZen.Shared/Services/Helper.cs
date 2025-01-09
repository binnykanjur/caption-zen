namespace CaptionZen.Shared.Services;

internal static class Helper {

    public static Task<string> ReadAllTextAsync(string outputDirectoryContentFileName) {
        var filePath = Path.Combine(AppContext.BaseDirectory,
            "Resources",
            outputDirectoryContentFileName
        );

        return System.IO.File.ReadAllTextAsync(filePath);
    }

    public static Microsoft.Extensions.AI.ChatRole ToChatRole(ChatMessageRole role) {
        switch (role) {
            case ChatMessageRole.System:
                return Microsoft.Extensions.AI.ChatRole.System;
            case ChatMessageRole.User:
                return Microsoft.Extensions.AI.ChatRole.User;
            case ChatMessageRole.Assistant:
                return Microsoft.Extensions.AI.ChatRole.Assistant;
            default:
                throw new NotSupportedException();
        }
    }

    public static ChatMessageRole FromChatRole(Microsoft.Extensions.AI.ChatRole role) {
        if (role == Microsoft.Extensions.AI.ChatRole.System) {
            return ChatMessageRole.System;
        } else if (role == Microsoft.Extensions.AI.ChatRole.User) {
            return ChatMessageRole.User;
        } else if (role == Microsoft.Extensions.AI.ChatRole.Assistant) {
            return ChatMessageRole.Assistant;
        }

        throw new NotSupportedException();
    }
}