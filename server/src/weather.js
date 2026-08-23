/**
 * Weather for Lake of the Woods, from the free open-meteo API. No account, no
 * key, nothing to expire.
 *
 * The standing rule is Jeffery's: weather is mentioned only when it changes a
 * plan. So this module hands over data plus judgement hints ("rain likely
 * during soccer practice") and the check-in decides whether it earns a line.
 */

const LAT = 38.33, LON = -77.79;   // Locust Grove / Lake of the Woods, VA
const URL = "https://api.open-meteo.com/v1/forecast"
  + `?latitude=${LAT}&longitude=${LON}`
  + "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_gusts_10m_max"
  + "&temperature_unit=fahrenheit&wind_speed_unit=mph"
  + "&timezone=America%2FNew_York&forecast_days=4";

// WMO weather codes, collapsed to what a family plan cares about.
const WMO = [
  [0, "clear"], [1, "mostly clear"], [2, "partly cloudy"], [3, "overcast"],
  [45, "fog"], [48, "fog"], [51, "drizzle"], [53, "drizzle"], [55, "drizzle"],
  [61, "light rain"], [63, "rain"], [65, "heavy rain"], [66, "freezing rain"],
  [67, "freezing rain"], [71, "snow"], [73, "snow"], [75, "heavy snow"],
  [77, "snow"], [80, "showers"], [81, "showers"], [82, "heavy showers"],
  [85, "snow showers"], [86, "snow showers"], [95, "thunderstorms"],
  [96, "thunderstorms with hail"], [99, "thunderstorms with hail"],
];
const describe = (code) => {
  let best = "unknown";
  for (const [c, name] of WMO) if (code >= c) best = name;
  return best;
};

let cache = { at: 0, days: null, error: null };

/** Next few days, cached for 30 minutes — the sky does not move faster. */
export async function forecast() {
  if (Date.now() - cache.at < 30 * 60e3 && (cache.days || cache.error)) return cache;
  try {
    const res = await fetch(URL, { signal: AbortSignal.timeout(10_000) });
    if (!res.ok) throw new Error(`open-meteo ${res.status}`);
    const j = await res.json();
    const d = j.daily;
    const days = d.time.map((date, i) => ({
      date,
      sky: describe(d.weather_code[i]),
      hi: Math.round(d.temperature_2m_max[i]),
      lo: Math.round(d.temperature_2m_min[i]),
      rain: d.precipitation_probability_max[i],   // percent
      gusts: Math.round(d.wind_gusts_10m_max[i]),
    }));
    cache = { at: Date.now(), days, error: null };
  } catch (e) {
    cache = { at: Date.now(), days: null, error: String(e?.message || e) };
  }
  return cache;
}

export function asWeatherLines(days) {
  return days.map((d) =>
    `${d.date}: ${d.sky}, ${d.lo}–${d.hi}°F` +
    (d.rain >= 30 ? `, ${d.rain}% chance of rain` : "") +
    (d.gusts >= 30 ? `, gusts to ${d.gusts}mph` : "")).join("\n");
}
