using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Library;

/// <summary>Pure validation rules for library metadata, copyright handling and links.</summary>
public static class LibraryRules
{
    public const int MaxTagLength = 60;

    /// <summary>
    /// Rights bases that allow a file to be redistributed to every signed-in user through a shared resource.
    /// A personal copy of a commercial book can only ever be stored privately.
    /// </summary>
    public static bool AllowsSharedDistribution(LibraryRightsBasis basis) =>
        basis is LibraryRightsBasis.OwnWork
            or LibraryRightsBasis.PublicDomain
            or LibraryRightsBasis.GovernmentPublication
            or LibraryRightsBasis.OpenLicense
            or LibraryRightsBasis.PermissionGranted;

    /// <summary>
    /// Bases whose redistribution depends on a specific licence, permission or publication terms
    /// must record that justification so reviewers can verify it.
    /// </summary>
    public static bool RequiresRightsNote(LibraryRightsBasis basis) =>
        basis is LibraryRightsBasis.GovernmentPublication
            or LibraryRightsBasis.OpenLicense
            or LibraryRightsBasis.PermissionGranted;

    /// <summary>Returns an error message when a file cannot be held under the given visibility and rights, otherwise null.</summary>
    public static string? ValidateFileRights(LibraryVisibility visibility, LibraryRightsBasis basis, string? rightsNote)
    {
        if (basis == LibraryRightsBasis.NotApplicable)
        {
            return "Declare the rights basis for this resource before attaching files.";
        }

        if (visibility == LibraryVisibility.Shared)
        {
            if (!AllowsSharedDistribution(basis))
            {
                return "Shared resources may only hold files that are your own work, public domain, government publications, openly licensed, or distributed with permission. Keep personal copies private or add an external link instead.";
            }

            if (RequiresRightsNote(basis) && string.IsNullOrWhiteSpace(rightsNote))
            {
                return "Record the licence, permission, or publication terms that allow this file to be shared.";
            }
        }

        return null;
    }

    /// <summary>Accepts absolute http/https URLs without embedded credentials.</summary>
    public static bool IsSafeWebUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
               !string.IsNullOrEmpty(uri.Host) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }

    /// <summary>Validates an ISBN-10 or ISBN-13 including its check digit. Hyphens and spaces are ignored.</summary>
    public static bool IsValidIsbn(string? value)
    {
        var isbn = NormalizeIsbn(value);
        if (isbn is null)
        {
            return false;
        }

        if (isbn.Length == 10)
        {
            var sum = 0;
            for (var index = 0; index < 10; index++)
            {
                var character = isbn[index];
                int digit;
                if (character is >= '0' and <= '9')
                {
                    digit = character - '0';
                }
                else if (index == 9 && character == 'X')
                {
                    digit = 10;
                }
                else
                {
                    return false;
                }

                sum += digit * (10 - index);
            }

            return sum % 11 == 0;
        }

        if (isbn.Length == 13 && isbn.All(char.IsAsciiDigit))
        {
            var sum = 0;
            for (var index = 0; index < 12; index++)
            {
                sum += (isbn[index] - '0') * (index % 2 == 0 ? 1 : 3);
            }

            var check = (10 - sum % 10) % 10;
            return check == isbn[12] - '0';
        }

        return false;
    }

    public static string? NormalizeIsbn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(value.Where(character => character is not ('-' or ' ')).ToArray())
            .ToUpperInvariant();
        return normalized.Length is 10 or 13 ? normalized : null;
    }

    /// <summary>Trims tags and checks that they are non-empty, short and unique (case-insensitive).</summary>
    public static bool TryNormalizeTags(IReadOnlyList<string>? tags, out IReadOnlyList<string> normalized)
    {
        normalized = [];
        if (tags is null)
        {
            return false;
        }

        var result = new List<string>(tags.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            var value = tag?.Trim();
            if (string.IsNullOrEmpty(value) || value.Length > MaxTagLength || !seen.Add(value))
            {
                return false;
            }

            result.Add(value);
        }

        normalized = result;
        return true;
    }
}
