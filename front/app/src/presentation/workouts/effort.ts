/** The exercise types that have an effort scale of their own (RIR, RPE, descriptive); any other uses the generic one. */
const TYPES_WITH_SCALE = new Set(["STRENGTH", "CARDIO", "STRETCHING"]);

/**
 * The translation keys an effort code reads from, most specific first: the scale of the exercise's type, then the
 * generic scale. Pass them to `t` together with the code as its default, so an unknown code is shown as it comes.
 */
export function effortKeysOf(code: string, type?: string): string[] {
  const generic = `efforts.${code}`;

  return type !== undefined && TYPES_WITH_SCALE.has(type)
    ? [`effortsByType.${type}.${code}`, generic]
    : [generic];
}
