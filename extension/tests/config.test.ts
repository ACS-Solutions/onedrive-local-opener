import { matchesGlob, shouldIntercept, Config } from '../src/config.js';

describe('matchesGlob', () => {
  it('matches * wildcard', () => {
    expect(matchesGlob('report.msg', '*')).toBe(true);
    expect(matchesGlob('anything.pdf', '*')).toBe(true);
  });

  it('matches *.ext pattern', () => {
    expect(matchesGlob('email.msg', '*.msg')).toBe(true);
    expect(matchesGlob('email.MSG', '*.msg')).toBe(true); // case-insensitive
    expect(matchesGlob('email.pdf', '*.msg')).toBe(false);
  });

  it('matches ? wildcard (single char)', () => {
    expect(matchesGlob('file1.txt', 'file?.txt')).toBe(true);
    expect(matchesGlob('file12.txt', 'file?.txt')).toBe(false);
  });

  it('does not treat dots as regex wildcards', () => {
    expect(matchesGlob('fileXpdf', '*.pdf')).toBe(false);
  });

  it('matches multiple extensions', () => {
    expect(matchesGlob('doc.docx', '*.doc*')).toBe(true);
    expect(matchesGlob('doc.docm', '*.doc*')).toBe(true);
  });

  it('matches literal filename', () => {
    expect(matchesGlob('readme.txt', 'readme.txt')).toBe(true);
    expect(matchesGlob('other.txt', 'readme.txt')).toBe(false);
  });
});

describe('shouldIntercept', () => {
  const base: Config = { enabled: true, includeGlobs: ['*'], excludeGlobs: [] };

  it('returns false when disabled', () => {
    const cfg: Config = { ...base, enabled: false };
    expect(shouldIntercept('https://example.sharepoint.com/file.msg', cfg)).toBe(false);
  });

  it('returns true for matching include', () => {
    expect(shouldIntercept('https://example.sharepoint.com/docs/report.msg', base)).toBe(true);
  });

  it('returns false when excluded', () => {
    const cfg: Config = { ...base, excludeGlobs: ['*.msg'] };
    expect(shouldIntercept('https://example.sharepoint.com/docs/report.msg', cfg)).toBe(false);
  });

  it('exclude overrides include', () => {
    const cfg: Config = { ...base, includeGlobs: ['*.msg'], excludeGlobs: ['*.msg'] };
    expect(shouldIntercept('https://example.sharepoint.com/docs/report.msg', cfg)).toBe(false);
  });

  it('strips query string before matching', () => {
    const cfg: Config = { ...base, includeGlobs: ['*.msg'] };
    expect(shouldIntercept('https://example.sharepoint.com/docs/report.msg?web=1', cfg)).toBe(true);
  });

  it('returns false when file does not match include pattern', () => {
    const cfg: Config = { ...base, includeGlobs: ['*.msg'] };
    expect(shouldIntercept('https://example.sharepoint.com/docs/report.pdf', cfg)).toBe(false);
  });
});
