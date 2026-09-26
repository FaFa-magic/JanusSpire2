// Run once with fmodstudiocl -script create-events.js Janus.fspro.
// The project retains the game's mixer GUIDs but publishes only Janus.bank.
if (studio.project.lookup("bank:/Janus"))
    throw new Error("Janus events already exist; rebuild with build-bank.ps1 instead.");
var bank = studio.project.create("Bank");
bank.name = "Janus";
bank.folder = studio.project.workspace.masterBankFolder;
var folder = studio.project.create("EventFolder");
folder.name = "janus";
folder.folder = studio.project.lookup("event:/sfx");
var files = [
    ["attack", "Janus_attacksfx.mp3"],
    ["cast", "Janus_castsfx.mp3"],
    ["death", "Janus_deathsfx.mp3"],
    ["character_select", "Janus_character_select.mp3"],
    ["character_transition", "Janus_character_transition.mp3"]
];
for (var i = 0; i < files.length; ++i) {
    var sourceRoot = studio.project.filePath.replace(/[^\/\\]+$/, "") + "../JanusSpire2/sfx/";
    var asset = studio.project.importAudioFile(sourceRoot + files[i][1]);
    if (!asset) throw new Error("Could not import " + files[i][1]);
    var event = studio.project.workspace.addEvent(files[i][0], false);
    event.folder = folder;
    event.relationships.banks.add(bank);
    event.mixerInput.output = studio.project.lookup("bus:/master/sfx");
    // FMOD authoring volume is in dB: 20 * log10(0.9).
    event.mixer.masterBus.volume = -0.9151498112135024;
    var track = event.addGroupTrack("Voice");
    var sound = track.addSound(event.timeline, "SingleSound", 0, asset.length);
    sound.audioFile = asset;
    console.log("Created " + files[i][0] + " (" + asset.length + " s)");
}
console.log("Saving Janus project");
if (!studio.project.save()) throw new Error("Could not save Janus FMOD project");
