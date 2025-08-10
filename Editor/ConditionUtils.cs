using System.Collections.Generic;
using System;
using System.Linq;
using ParamType = UnityEngine.AnimatorControllerParameterType;
using UnityEditor.Animations;
using top.kuriko.Common;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class ConditionUtils
    {

        public static readonly ConditionMode[] Modes = (ConditionMode[])Enum.GetValues(typeof(ConditionMode));
        static readonly IReadOnlyDictionary<ConditionMode, int> ModeIndexes
        = Modes.Select((v, i) => (v, i)).ToDictionary(v => v.v, v => v.i);

        public static int GetIndex(this ConditionMode v) => ModeIndexes[v];

        public static string GetOperator(this ConditionMode v) => v switch
        {
            ConditionMode.If => "if",
            ConditionMode.IfNot => "not",
            ConditionMode.Greater => ">",
            ConditionMode.GreaterEquals => ">=",
            ConditionMode.Less => "<",
            ConditionMode.LessEquals => "<=",
            ConditionMode.Equals => "==",
            ConditionMode.NotEqual => "!=",
            ConditionMode.InRange => "in",
            ConditionMode.OutOfRange => "out",
            _ => "",
        };

        public static readonly string[] Operators
        = Modes.Select(GetOperator).ToArray();

        public static ConditionMode Invert(this ConditionMode v) => v switch
        {
            ConditionMode.If => ConditionMode.IfNot,
            ConditionMode.IfNot => ConditionMode.If,
            ConditionMode.Greater => ConditionMode.LessEquals,
            ConditionMode.GreaterEquals => ConditionMode.Less,
            ConditionMode.Less => ConditionMode.GreaterEquals,
            ConditionMode.LessEquals => ConditionMode.Greater,
            ConditionMode.Equals => ConditionMode.NotEqual,
            ConditionMode.NotEqual => ConditionMode.Equals,
            ConditionMode.InRange => ConditionMode.OutOfRange,
            ConditionMode.OutOfRange => ConditionMode.InRange,
            _ => throw new ArgumentException(),
        };

        public static Condition Invert(this Condition c) => new(c.ParameterName, c.Mode.Invert(), c.Threshold, c.Threshold2);

        public static readonly IReadOnlyDictionary<ParamType, ConditionMode[]> TypedConditions
        = Enum.GetValues(typeof(ParamType))
            .Cast<ParamType>()
            .Select(t => (t, cs: t switch
            {
                ParamType.Float => new[]
                {
                    ConditionMode.Greater,
                    ConditionMode.GreaterEquals,
                    ConditionMode.Less,
                    ConditionMode.LessEquals,
                    ConditionMode.Equals,
                    ConditionMode.NotEqual,
                    ConditionMode.InRange,
                    ConditionMode.OutOfRange,
                },
                ParamType.Int => new[]
                {
                    ConditionMode.Greater,
                    ConditionMode.GreaterEquals,
                    ConditionMode.Less,
                    ConditionMode.LessEquals,
                    ConditionMode.Equals,
                    ConditionMode.NotEqual,
                    ConditionMode.InRange,
                    ConditionMode.OutOfRange,
                },
                ParamType.Bool or ParamType.Trigger => new[]
                {
                    ConditionMode.If,
                    ConditionMode.IfNot,
                },
                _ => null,
            }))
            .Where(t => t.cs != null)
            .ToDictionary(t => t.t, t => t.cs);

        public static ConditionMode[] GetConditions(this ParamType type) => TypedConditions[type];

        public static readonly IReadOnlyDictionary<ParamType, string[]> TypedConditionOperators
        = TypedConditions.ToDictionary(kv => kv.Key, kv => kv.Value.Select(GetOperator).ToArray());

        public static string[] GetConditionOperators(this ParamType type)
        => TypedConditionOperators[type];

        public static ConditionMode GetDefaultCondition(this ParamType type) => TypedConditions.TryGetValue(type, out var ms) ? ms[0] : default;

        public static float PopupWidth = 40;

        public static ConditionMode ValidOrDefault(this ConditionMode mode, ParamType type)
        => type.GetConditions().Contains(mode) ? mode : type.GetDefaultCondition();

        public static float FloatEqualsOffset { get; set; } = 0.0001f;

        public static AnimatorConditionMode ToAnimatorCondition(this ConditionMode mode) => mode switch
        {
            ConditionMode.If => AnimatorConditionMode.If,
            ConditionMode.IfNot => AnimatorConditionMode.IfNot,
            ConditionMode.Greater => AnimatorConditionMode.Greater,
            ConditionMode.Less => AnimatorConditionMode.Less,
            ConditionMode.Equals => AnimatorConditionMode.Equals,
            ConditionMode.NotEqual => AnimatorConditionMode.NotEqual,
            _ => throw new ArgumentException(),
        };

        public static AnimatorCondition? ToAnimatorCondition(this Condition c)
        => c.Mode switch
        {
            ConditionMode.If 
            or ConditionMode.IfNot 
            or ConditionMode.Greater 
            or ConditionMode.Less 
            or ConditionMode.Equals 
            or ConditionMode.NotEqual => new()
            {
                parameter = c.ParameterName,
                mode = c.Mode.ToAnimatorCondition(),
                threshold = c.Threshold,
            },
            _ => null
        };

        public static (IReadOnlyList<AnimatorCondition> Conditions, bool and) ToAnimatorConditions(this Condition c, ParamType type)
        {
            var (mode, val, val2) = (c.Mode, c.Threshold, c.Threshold2);
            var lst = new List<AnimatorCondition>();
            void add(ConditionMode mode, float v) => lst.Add(new()
            {
                parameter = c.ParameterName,
                mode = mode.ToAnimatorCondition(),
                threshold = v,
            });
            var and = false;
            switch (type)
            {
                case ParamType.Float:
                    switch (mode)
                    {
                        case ConditionMode.GreaterEquals:
                            add(ConditionMode.Greater, val - FloatEqualsOffset);
                            break;
                        case ConditionMode.LessEquals:
                            add(ConditionMode.Less, val + FloatEqualsOffset);
                            break;
                        case ConditionMode.Equals:
                            if (val <= 0)
                                add(ConditionMode.Less, FloatEqualsOffset);
                            else if (val >= 1 - FloatEqualsOffset)
                                add(ConditionMode.Greater, 1 - FloatEqualsOffset);
                            else
                            {
                                add(ConditionMode.Greater, val - FloatEqualsOffset);
                                add(ConditionMode.Less, val + FloatEqualsOffset);
                                and = true;
                            }
                            break;
                        case ConditionMode.NotEqual:
                            if (val <= 0)
                                add(ConditionMode.Greater, 0);
                            else if (val >= 1 - FloatEqualsOffset)
                                add(ConditionMode.Less, 1);
                            else
                            {
                                add(ConditionMode.Greater, val + FloatEqualsOffset);
                                add(ConditionMode.Less, val - FloatEqualsOffset);
                            }
                            break;
                        case ConditionMode.Greater:
                        case ConditionMode.Less:
                            add(mode, val);
                            break;
                        case ConditionMode.InRange:
                            if (val.ApproximateEquals(val2))
                                goto case ConditionMode.Equals;
                            add(ConditionMode.Greater, MathF.Min(val, val2) - FloatEqualsOffset);
                            add(ConditionMode.Less, MathF.Max(val, val2) + FloatEqualsOffset);
                            and = true;
                            break;
                        case ConditionMode.OutOfRange:
                            if (val.ApproximateEquals(val2))
                                goto case ConditionMode.NotEqual;
                            add(ConditionMode.Less, MathF.Min(val, val2));
                            add(ConditionMode.Greater, MathF.Max(val, val2));
                            break;
                    }
                    break;
                case ParamType.Int:
                    switch (mode)
                    {
                        case ConditionMode.GreaterEquals:
                            if ((int)val == 0)
                            {
                                add(ConditionMode.Equals, 0);
                                add(ConditionMode.NotEqual, 0);
                            }
                            else
                            {
                                val = ((int)val) - 1;
                                goto case ConditionMode.Greater;
                            }
                            break;
                        case ConditionMode.LessEquals:
                            if ((int)val == 255)
                            {
                                add(ConditionMode.Equals, 0);
                                add(ConditionMode.NotEqual, 0);
                            }
                            else
                            {
                                val = ((int)val) + 1;
                                goto case ConditionMode.Less;
                            }
                            break;
                        case ConditionMode.Greater:
                        case ConditionMode.Less:
                        case ConditionMode.Equals:
                        case ConditionMode.NotEqual:
                            add(mode, val);
                            break;
                        case ConditionMode.InRange:
                            add(ConditionMode.Greater, MathF.Min(val, val2) - 1);
                            add(ConditionMode.Less, MathF.Max(val, val2) + 1);
                            and = true;
                            break;
                        case ConditionMode.OutOfRange:
                            add(ConditionMode.Less, MathF.Min(val, val2));
                            add(ConditionMode.Greater, MathF.Max(val, val2));
                            break;
                    }
                    break;
                case ParamType.Bool:
                case ParamType.Trigger:
                    add(mode, 1);
                    break;
            }
            return (lst.ToArray(), and);
        }

        public static List<List<AnimatorCondition>> ToAnimatorConditions(this IEnumerable<Condition> conditions, Func<string, ParamType?> getType)
        {
            var cs = conditions.SelectMany(c =>
            {
                var (acs, and) = ToAnimatorConditions(c, getType(c.ParameterName) ?? ParamType.Float);
                return and ? acs.Select(c => (IReadOnlyList<AnimatorCondition>)new[] { c }) : new[] { acs };
            });
            static string GetConditionKey(AnimatorCondition c) => $"{c.parameter}\0{c.mode}\0{MathF.Round(c.threshold, 4)}";
            var andcs = cs
                .Where(x => x.Count == 1)
                .SelectMany(x => x)
                .DistinctBy(GetConditionKey)
                .ToList();
            var orcs = cs
                .Where(x => x.Count > 1)
                .ToList()
                .Cartesian();
            var lst = orcs.Count == 0 ? new(new[] { andcs }) : orcs.Select(x => x
                .Concat(andcs)
                .DistinctBy(GetConditionKey)
                .ToList()
                ).ToList();
            return lst;
        }

        public static List<List<AnimatorCondition>> ToAnimatorConditions(this IEnumerable<Condition> conditions, Func<string, ParamType> getType)
        => ToAnimatorConditions(conditions, p => (ParamType?)getType(p));

        public record RangeF(float Min, float Max)
        {
            //public bool InRange(RangeF other)
            //=> (Min >= other.Min && Min <= other.Max)
            //    || (Max >= other.Min && Max <= other.Max);

            public bool InRange(RangeF other, Func<float, float, int> compare)
            => (compare(Min, other.Min) >= 0 && compare(Min, other.Max) <= 0)
                || (compare(Max, other.Min) >= 0 && compare(Max, other.Max) <= 0);
        }
        public record Range(float Min, float Max)
        {
            public bool InRange(Range other)
            => (Min >= other.Min && Min <= other.Max)
                || (Max >= other.Min && Max <= other.Max);
        }

        public static (bool IsRangeFull, bool IsOverlapping) GetRangeInfo(
            this IEnumerable<(ConditionMode mode, float val, float val2)> conditions,
            ParamType type,
            int floatDigits = 5)
        {
            var rangeFull = false;
            var overlapping = false;
            switch (type)
            {
                case ParamType.Float:
                    {
                        var mv = (int)MathF.Pow(10, floatDigits);
                        var variance = 1f / mv;
                        var min = 0;
                        var max = 1 + variance;
                        var ranges = conditions
                            .SelectMany(c =>
                            {
                                var (mode, val, val2) = c;
                                var maxv = MathF.Min(val + variance, max);
                                var minv = MathF.Max(val - variance, min);
                                return mode switch
                                {
                                    ConditionMode.Greater
                                        => new RangeF[] { new(maxv, max) },
                                    ConditionMode.GreaterEquals
                                        => new RangeF[] { new(val, 1) },
                                    ConditionMode.Less
                                        => new RangeF[] { new(min, minv) },
                                    ConditionMode.LessEquals
                                        => new RangeF[] { new(min, val) },
                                    ConditionMode.Equals
                                        => new RangeF[] { new(minv, maxv) },
                                    ConditionMode.NotEqual
                                        => new RangeF[] { new(min, minv), new(maxv, max) },
                                    ConditionMode.InRange
                                        => new RangeF[] { new(MathF.Min(val, val2), MathF.Max(val, val2)) },
                                    ConditionMode.OutOfRange => new RangeF[] {
                                        new(min, MathF.Max(MathF.Min(val, val2) - variance, min)),
                                        new(MathF.Min(MathF.Max(val, val2) + variance, max), max),
                                    },
                                    _ => throw new ArgumentException(),
                                };
                            })
                            .OrderBy(c => c.Min)
                            .ThenBy(c => c.Max)
                            .ToList();
                        int compare(float x, float y) => (int)((MathF.Round(x, floatDigits) - MathF.Round(y, floatDigits)) * mv * 10);
                        var fullrange = new RangeF(ranges[0].Min, ranges[0].Max);
                        for (var i = 1; i < ranges.Count; i++)
                        {
                            var range = ranges[i];
                            if (!overlapping)
                            {
                                if (ranges.Take(i).Any(r => range.InRange(r, compare)))
                                    overlapping = true;
                            }
                            if (!rangeFull)
                            {
                                if (compare(range.Min, fullrange.Min) < 0)
                                    fullrange = fullrange with { Min = range.Min };
                                if (compare(range.Max, fullrange.Max) > 0)
                                    fullrange = fullrange with { Max = range.Max };
                                if (fullrange.Min <= 0 && fullrange.Max >= 1)
                                    rangeFull = true;
                            }
                            if (rangeFull && overlapping)
                                break;
                        }
                    }
                    break;
                case ParamType.Int:
                    {
                        var min = 0;
                        var max = 255;
                        var ranges = conditions
                            .Select(c => (c.mode, val: (int)MathF.Round(c.val), val2: (int)MathF.Round(c.val2)))
                            .SelectMany(c =>
                            {
                                var (mode, val, val2) = c;
                                return mode switch
                                {
                                    ConditionMode.Greater
                                        => new Range[] { new(Math.Min(val + 1, max), max) },
                                    ConditionMode.GreaterEquals
                                        => new Range[] { new(val, 1) },
                                    ConditionMode.Less
                                        => new Range[] { new(min, Math.Max(val - 1, min)) },
                                    ConditionMode.LessEquals
                                        => new Range[] { new(min, val) },
                                    ConditionMode.Equals
                                        => new Range[] { new(val, val) },
                                    ConditionMode.NotEqual
                                        => new Range[] { new(min, val), new(val, max) },
                                    ConditionMode.InRange
                                        => new Range[] { new(val, val2) },
                                    ConditionMode.OutOfRange
                                        => new Range[] {
                                            new(min, val <= val2 ? val : val2),
                                            new(val <= val2 ? val2 : val, max),
                                        },
                                    _ => throw new ArgumentException(),
                                };
                            })
                            .OrderBy(c => c.Min)
                            .ThenBy(c => c.Max)
                            .ToList();
                        var fullrange = new Range(ranges[0].Min, ranges[0].Max);
                        for (var i = 1; i < ranges.Count; i++)
                        {
                            var range = ranges[i];
                            if (!overlapping)
                            {
                                if (ranges.Take(i).Any(range.InRange))
                                    overlapping = true;
                            }
                            if (!rangeFull)
                            {
                                if (range.Min < fullrange.Min)
                                    fullrange = fullrange with { Min = range.Min };
                                if (range.Max > fullrange.Max)
                                    fullrange = fullrange with { Max = range.Max };
                                if (fullrange.Min <= 0 && fullrange.Max >= 1)
                                    rangeFull = true;
                            }
                            if (rangeFull && overlapping)
                                break;
                        }
                    }
                    break;
                case ParamType.Bool:
                case ParamType.Trigger:
                    {
                        var gs = conditions.GroupBy(c => c.mode);
                        rangeFull = gs.Count() > 1;
                        overlapping = gs.Any(g => g.Count() > 1);
                    }
                    break;
            }
            return (rangeFull, overlapping);
        }
    }
}
