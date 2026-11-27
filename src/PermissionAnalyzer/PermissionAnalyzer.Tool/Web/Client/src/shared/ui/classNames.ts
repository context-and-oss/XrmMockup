/** Combine CSS-module classes with optional state classes. */
export const cx = (...classes: (string | false | undefined)[]) => classes.filter(Boolean).join(' ');
