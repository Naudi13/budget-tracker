/**
 * Reads an amount as a person in Colombia types it: digits with one decimal
 * separator, comma or point, and at most two decimals ("85,50", "85.5", "1200").
 * Thousands separators are not accepted: "1.200" would be ambiguous.
 * Returns null for anything else.
 */
export function parseAmount(text: string): number | null {
  const match = /^\s*(\d{1,12})(?:[.,](\d{1,2}))?\s*$/.exec(text);
  if (!match) {
    return null;
  }
  const cents = Number(match[1]) * 100 + Number((match[2] ?? '').padEnd(2, '0'));
  return cents / 100;
}

/** Today's date as the API expects it, `yyyy-MM-dd`, in the user's own time zone. */
export function todayIso(now: Date = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}
