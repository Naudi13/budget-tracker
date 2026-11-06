import { parseAmount, todayIso } from './money';

describe('parseAmount', () => {
  it.each([
    ['85,50', 85.5],
    ['85.50', 85.5],
    ['85,5', 85.5],
    ['1200', 1200],
    [' 0,01 ', 0.01],
    ['999999999999,99', 999999999999.99],
  ])('reads %s as %d', (text, value) => {
    expect(parseAmount(text)).toBe(value);
  });

  it.each(['', 'abc', '1.200,50', '10,005', '-5', '1e3', '12,', ',5'])('refuses %s', (text) => {
    expect(parseAmount(text)).toBeNull();
  });

  it('keeps cents exact, with no floating point drift', () => {
    expect(parseAmount('0,1')! + parseAmount('0,2')!).toBeCloseTo(0.3, 10);
    expect(parseAmount('19,99')).toBe(19.99);
  });
});

describe('todayIso', () => {
  it('formats the local date, padded', () => {
    expect(todayIso(new Date(2026, 0, 5, 23, 59))).toBe('2026-01-05');
  });
});
