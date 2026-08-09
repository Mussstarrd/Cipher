class_name Chart
extends RefCounted
## Levels are data. A chart is JSON:
## {
##   "bpm": float, "offset_ms": int, "swing_percent": float,
##   "audio_file": string,
##   "stems": {"instrumental": path, "player_drums": path, "escalation": [paths]},
##   "events": [{"beat": float, "type": string,
##               "nudge_ms": int, "lane": int, "duration_beats": float}]
## }
## nudge_ms, lane, duration_beats default to 0. Fixed BPM only — no tempo maps.

var bpm := 90.0
var offset_ms := 0
var swing_percent := 50.0
var audio_file := ""
var backdrop := "alley"
var stems: Dictionary = {}
var events: Array[Dictionary] = []


static func load_from_file(path: String) -> Chart:
	if not FileAccess.file_exists(path):
		push_error("Chart not found: " + path)
		return null
	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		push_error("Chart unreadable: " + path)
		return null
	var data: Variant = JSON.parse_string(f.get_as_text())
	if typeof(data) != TYPE_DICTIONARY:
		push_error("Chart is not valid JSON: " + path)
		return null
	return from_dict(data)


static func from_dict(d: Dictionary) -> Chart:
	var c := Chart.new()
	c.bpm = float(d.get("bpm", 90.0))
	c.offset_ms = int(d.get("offset_ms", 0))
	c.swing_percent = float(d.get("swing_percent", 50.0))
	c.audio_file = str(d.get("audio_file", ""))
	c.backdrop = str(d.get("backdrop", "alley"))
	c.stems = d.get("stems", {})
	for raw in d.get("events", []):
		if typeof(raw) != TYPE_DICTIONARY:
			continue
		c.events.append({
			"beat": float(raw.get("beat", 0.0)),
			"type": str(raw.get("type", "enemy")),
			"nudge_ms": int(raw.get("nudge_ms", 0)),
			"lane": int(raw.get("lane", 0)),
			"duration_beats": float(raw.get("duration_beats", 0.0)),
		})
	c.sort_events()
	return c


func to_dict() -> Dictionary:
	return {
		"bpm": bpm,
		"offset_ms": offset_ms,
		"swing_percent": swing_percent,
		"audio_file": audio_file,
		"backdrop": backdrop,
		"stems": stems,
		"events": events,
	}


func save(path: String) -> bool:
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return false
	f.store_string(JSON.stringify(to_dict(), "  "))
	return true


func sort_events() -> void:
	events.sort_custom(
		func(a: Dictionary, b: Dictionary) -> bool:
			return float(a["beat"]) < float(b["beat"]))


## Difficulty filter: one chart serves all difficulties — an event belongs
## to a grid when its beat sits on that grid's resolution.
func events_for_step(step: float) -> Array[Dictionary]:
	var out: Array[Dictionary] = []
	for e in events:
		var q: float = float(e["beat"]) / step
		if absf(q - roundf(q)) < 0.001:
			out.append(e)
	return out


func end_beat() -> float:
	var m := 0.0
	for e in events:
		m = maxf(m, float(e["beat"]))
	return m
