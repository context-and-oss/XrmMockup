/** Parse comma-separated report filters without changing their CLI semantics. */
export const values = (text: string): string[] => text.split(',').map(value => value.trim()).filter(Boolean);
