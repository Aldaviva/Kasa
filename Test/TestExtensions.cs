using FakeItEasy;

namespace Test;

public static class TestExtensions {

    public static object HasProperty<T>(this IArgumentConstraintManager<object> manager, string propertyName, T? expected) {
        return manager.Matches(o => MatchProperty(o, propertyName, expected));
    }

    public static T Matches<T>(this IArgumentConstraintManager<T> manager, Action<T> assertion) {
        return manager.Matches(subject => {
            try {
                assertion(subject);
                return true;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                return false;
            }
        }, "custom assertion");
    }

    private static bool MatchProperty<T>(object? instance, string propertyName, T expected) {
        return Equals(instance?.GetType().GetProperty(propertyName, typeof(T))?.GetValue(instance), expected);
    }

}