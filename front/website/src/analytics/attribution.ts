/** Where a visit comes from: its UTM parameters and the page that linked to it. */
export interface Attribution {
  utm_source?: string;
  utm_medium?: string;
  utm_campaign?: string;
  utm_term?: string;
  utm_content?: string;
  referrer?: string;
}

const UTM_KEYS = ["utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content"] as const;

/** The attribution of a visit, from its query string and referrer; empty values are left out. */
export function attributionOf(search: string, referrer: string): Attribution {
  const params = new URLSearchParams(search);
  const attribution: Attribution = {};

  for (const key of UTM_KEYS) {
    const value = params.get(key)?.trim();
    if (value) {
      attribution[key] = value;
    }
  }
  if (referrer) {
    attribution.referrer = referrer;
  }

  return attribution;
}

let current: Attribution | null = null;

/** The visit's attribution, read once on the first call and kept in memory; nothing is stored. */
export function visitAttribution(): Attribution {
  if (current === null) {
    current =
      typeof window === "undefined" ? {} : attributionOf(window.location.search, document.referrer);
  }
  return current;
}

/** Forgets the visit's attribution; for tests only. */
export function resetAttribution(): void {
  current = null;
}
