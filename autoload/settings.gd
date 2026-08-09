extends Node
## Settings (autoload) — persistent player settings in user://settings.cfg:
## difficulty and the tap calibration offset.

const PATH := "user://settings.cfg"
const DIFFICULTIES: Array[String] = ["easy", "medium", "hard"]

## Song registry: id, menu title, chart path.
const SONGS: Array[Dictionary] = [
	{"id": "dragon", "title": "DRAGON'S BREATH", "chart": "res://charts/dragon.chart.json"},
	{"id": "syrup", "title": "SYRUP ERHU", "chart": "res://charts/syrup.chart.json"},
	{"id": "aintshit", "title": "MALL SHOW", "chart": "res://charts/aintshit.chart.json"},
]

var selected_song := "dragon"

var difficulty := "easy"
var calibration_offset_ms := 0.0
var has_calibration := false
## Shifts where cues are DRAWN relative to the audio clock (positive =
## visuals later). Compensates devices that misreport output latency.
## Adjust live from the debug overlay. Judgment is unaffected — tap
## calibration handles the input side.
var av_offset_ms := 0.0


func _ready() -> void:
	load_settings()


func load_settings() -> void:
	var cf := ConfigFile.new()
	if cf.load(PATH) != OK:
		return
	difficulty = cf.get_value("game", "difficulty", "easy")
	calibration_offset_ms = cf.get_value("calibration", "offset_ms", 0.0)
	has_calibration = cf.get_value("calibration", "done", false)
	av_offset_ms = cf.get_value("calibration", "av_offset_ms", 0.0)
	selected_song = cf.get_value("game", "song", "dragon")


func save_settings() -> void:
	var cf := ConfigFile.new()
	cf.set_value("game", "difficulty", difficulty)
	cf.set_value("calibration", "offset_ms", calibration_offset_ms)
	cf.set_value("calibration", "done", has_calibration)
	cf.set_value("calibration", "av_offset_ms", av_offset_ms)
	cf.set_value("game", "song", selected_song)
	cf.save(PATH)


## Difficulty = grid resolution, in beats.
## Easy: quarters. Medium: eighths. Hard: sixteenths (swing applies).
func grid_step() -> float:
	if difficulty == "easy":
		return 1.0
	if difficulty == "medium":
		return 0.5
	return 0.25


func song_entry() -> Dictionary:
	for s in SONGS:
		if s["id"] == selected_song:
			return s
	return SONGS[0]


func cycle_song() -> Dictionary:
	var idx := 0
	for i in SONGS.size():
		if SONGS[i]["id"] == selected_song:
			idx = i
			break
	var next: Dictionary = SONGS[(idx + 1) % SONGS.size()]
	selected_song = next["id"]
	save_settings()
	return next


func cycle_difficulty() -> String:
	var i := (DIFFICULTIES.find(difficulty) + 1) % DIFFICULTIES.size()
	difficulty = DIFFICULTIES[i]
	save_settings()
	return difficulty
