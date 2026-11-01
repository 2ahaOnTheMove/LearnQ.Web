using System.ComponentModel.DataAnnotations;
using LearnQ.Application.Common.Constants;

namespace LearnQ.Presentation.Components.Onboarding;

public sealed class StudentOnboardingInput : IValidatableObject
{
    public bool? ForSelf { get; set; }

    public bool GuardianConfirmed { get; set; }

    public LearnerInput SelfLearner { get; set; } = new()
    {
        RequiresNames = false,
        EnforceAccountHolderMinAge = true
    };

    public List<LearnerInput> SomeoneElseLearners { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ForSelf is null)
        {
            yield return new ValidationResult("Please select who this account is for.", [nameof(ForSelf)]);
            yield break;
        }

        if (ForSelf == true)
        {
            SelfLearner.RequiresNames = false;
            SelfLearner.EnforceAccountHolderMinAge = true;

            var results = new List<ValidationResult>();
            var context = new ValidationContext(SelfLearner);
            Validator.TryValidateObject(SelfLearner, context, results, validateAllProperties: true);
            foreach (var r in results)
            {
                yield return r;
            }
        }
        else
        {
            if (!GuardianConfirmed)
            {
                yield return new ValidationResult(
                    "You must confirm you are at least 16 years old and the parent or legal guardian of every learner you add.",
                    [nameof(GuardianConfirmed)]);
            }

            if (SomeoneElseLearners.Count == 0)
            {
                yield return new ValidationResult("Please add at least one learner.", [nameof(SomeoneElseLearners)]);
            }

            if (SomeoneElseLearners.Count > OnboardingConstants.MaxLearners)
            {
                yield return new ValidationResult(
                    $"You can add up to {OnboardingConstants.MaxLearners} learners.",
                    [nameof(SomeoneElseLearners)]);
            }

            foreach (var learner in SomeoneElseLearners)
            {
                learner.RequiresNames = true;
                learner.EnforceAccountHolderMinAge = false;

                var results = new List<ValidationResult>();
                var context = new ValidationContext(learner);
                Validator.TryValidateObject(learner, context, results, validateAllProperties: true);
                foreach (var r in results)
                {
                    yield return r;
                }
            }
        }
    }

    public bool HasData()
    {
        if (ForSelf == true)
        {
            return SelfLearner.HasData();
        }

        if (ForSelf == false)
        {
            return GuardianConfirmed || SomeoneElseLearners.Count > 0;
        }

        return false;
    }
}
