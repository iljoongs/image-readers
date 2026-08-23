namespace ImageTopicViewer.Services;

/// <summary>
/// doc/04-topic-management.md "이름 유효성 검증" 규칙.
/// </summary>
public static class TopicNameValidator
{
    private static readonly char[] InvalidChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static bool IsValid(string name, IEnumerable<string> existingSiblingNames, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "이름을 입력해주세요.";
            return false;
        }

        if (name.IndexOfAny(InvalidChars) >= 0)
        {
            errorMessage = "다음 문자는 사용할 수 없습니다: \\ / : * ? \" < > |";
            return false;
        }

        if (name.EndsWith('.') || name.EndsWith(' '))
        {
            errorMessage = "이름 끝에 마침표나 공백을 사용할 수 없습니다.";
            return false;
        }

        if (ReservedNames.Contains(name))
        {
            errorMessage = $"'{name}'은(는) Windows 예약 이름이라 사용할 수 없습니다.";
            return false;
        }

        if (existingSiblingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            errorMessage = "이미 같은 이름이 존재합니다.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
