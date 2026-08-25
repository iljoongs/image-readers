namespace ImageTopicViewer.Services;

/// <summary>
/// 문자열에 포함된 연속된 숫자를 문자 단위가 아니라 실제 숫자 값으로 비교하는 "자연 정렬".
/// 예: "2.jpg" &lt; "10.jpg", "1화" &lt; "2화" &lt; "10화" (일반 문자열 정렬이면 "10"이 "2"보다 앞에 옴).
/// 이미지 파일 읽기 순서(03-data-storage.md)와 주제 이름 정렬(04-topic-management.md)에 사용한다.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string?>
{
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var i = 0;
        var j = 0;

        while (i < x.Length && j < y.Length)
        {
            var cx = x[i];
            var cy = y[j];

            if (char.IsDigit(cx) && char.IsDigit(cy))
            {
                var startX = i;
                while (i < x.Length && char.IsDigit(x[i]))
                {
                    i++;
                }

                var startY = j;
                while (j < y.Length && char.IsDigit(y[j]))
                {
                    j++;
                }

                var digitsX = x.AsSpan(startX, i - startX).TrimStart('0');
                var digitsY = y.AsSpan(startY, j - startY).TrimStart('0');

                // 자릿수가 다르면(선행 0 제거 후) 자릿수가 적은 쪽이 더 작은 숫자다.
                if (digitsX.Length != digitsY.Length)
                {
                    return digitsX.Length - digitsY.Length;
                }

                var numberCompare = digitsX.CompareTo(digitsY, StringComparison.Ordinal);
                if (numberCompare != 0)
                {
                    return numberCompare;
                }

                // 숫자 값이 같으면(예: "007" vs "7") 다음 부분 비교로 넘어간다.
            }
            else
            {
                if (cx != cy)
                {
                    return cx.CompareTo(cy);
                }

                i++;
                j++;
            }
        }

        return (x.Length - i) - (y.Length - j);
    }
}
