using LearnQ.Application.Common.Enums;

namespace LearnQ.Presentation.Components.Shared;

public static class EnumDisplayHelper
{
    public static readonly IReadOnlyDictionary<Gender, string> GenderLabels =
        new Dictionary<Gender, string>
        {
            [Gender.PreferNotToSay] = "Prefer not to say",
            [Gender.Male] = "Male",
            [Gender.Female] = "Female"
        };

    public static readonly IReadOnlyDictionary<GenderRestriction, string> TeacherGenderRestrictionLabels =
        new Dictionary<GenderRestriction, string>
        {
            [GenderRestriction.None] = "No restriction",
            [GenderRestriction.MaleOnly] = "Male students only",
            [GenderRestriction.FemaleOnly] = "Female students only"
        };

    public static readonly IReadOnlyDictionary<GenderRestriction, string> LearnerPreferredTeacherGenderLabels =
        new Dictionary<GenderRestriction, string>
        {
            [GenderRestriction.None] = "No preference",
            [GenderRestriction.MaleOnly] = "Male teachers only",
            [GenderRestriction.FemaleOnly] = "Female teachers only"
        };
}
