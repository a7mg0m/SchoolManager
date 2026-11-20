import { TranslateService } from '@ngx-translate/core';

/**
 * Text of an API error key of the clinic (e.g. "DoseAlreadyRecorded" -> clinic.errors.DoseAlreadyRecorded),
 * or the fallback message when the key is unknown / missing.
 */
export function clinicApiError(translate: TranslateService, key: string | null | undefined, fallbackKey: string): string {
  if (key) {
    const translationKey = `clinic.errors.${key}`;
    const text = translate.instant(translationKey);
    if (text !== translationKey) return text;
  }
  return translate.instant(fallbackKey);
}
