#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Progression;

namespace Cipher.Game.UI.Comic
{
    /// <summary>What the bootstrap may have to react to after a frame of kit input.</summary>
    public enum KitScreenAction { None, Closed, Equipped, Unequipped, Scrapped, Refused }

    /// <summary>
    /// THE KIT SCREEN: the paper doll wired to the real <see cref="Inventory"/>.
    ///
    /// Three jobs, in the order they matter:
    ///
    ///   1. Translate. <see cref="KitPageLayout"/> speaks in strings and one number on purpose, so
    ///      the page never learns what an affix is. The mapping lives here and nowhere else.
    ///   2. Own its input, on a pad first. Left stick scrolls; B closes; the button that opened the
    ///      page is consumed by this screen and therefore cannot close it.
    ///   3. Say, on the page itself, where gear comes from — the owner's actual complaint was not
    ///      that the screen was ugly, it was "I don't understand where I'm picking up gear from how
    ///      my inventory works".
    ///
    /// Everything except <see cref="Draw"/> and <see cref="HandleInput"/> is plain C# and tested.
    /// </summary>
    public sealed class KitScreen
    {
        /// <summary>
        /// Slot order down the doll: four on the left, four on the right, matching
        /// KitPageLayout.Anchor's hand-placed points (head, chest, hand, foot / neck, hand, waist,
        /// foot). Worn things go where they are worn, which is the entire argument for a doll over a
        /// list.
        /// </summary>
        public static readonly Slot[] DollOrder =
        {
            Slot.Helm, Slot.Vest, Slot.Gloves, Slot.Boots,
            Slot.DogTag, Slot.Weapon, Slot.CharmA, Slot.CharmB,
        };

        /// <summary>Player-facing names for <see cref="DollOrder"/>, index for index.</summary>
        public static readonly string[] SlotNames =
        {
            "Helm", "Vest", "Gloves", "Boots",
            "Dog Tag", "Emitter", "Charm A", "Charm B",
        };

        /// <summary>The heading across the top of the page.</summary>
        public const string Title = "KIT  —  what you are carrying";

        /// <summary>
        /// The one sentence at the foot. This is the answer to the owner's question, printed every
        /// time the page opens rather than once in a tutorial he will not read.
        /// </summary>
        public const string Explain =
            "Gear drops off the signed you put down. Clearly worse pieces are scrapped for scrip where they fall; "
            + "the rest waits in your pack until you come here.";

        private readonly Loadout _loadout;
        private readonly IComicScreenHost _host;
        private readonly Action? _changed;
        private readonly ComicInput _input = new ComicInput();

        private KitPageLayout _page;

        public KitScreen(Loadout loadout, IComicScreenHost? host = null, Action? onChanged = null)
        {
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            _host = host ?? NullComicHost.Instance;
            _changed = onChanged;
            _page = BuildPage();
        }

        // ------------------------------------------------------------------ the tiny surface

        public bool IsOpen { get; private set; }

        /// <summary>The laid-out page. Exposed for tests and for the screenshot harness.</summary>
        public KitPageLayout Page => _page;

        /// <summary>
        /// Open it. There is deliberately no Toggle: an open page consumes every input, so the
        /// bootstrap never reaches its own opening binding while this is up, and B is the only exit.
        /// </summary>
        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            Rebuild();
            _input.Reset();
        }

        public void Hide() => IsOpen = false;

        // ------------------------------------------------------------------ translation

        /// <summary>Everything worn, as the page's generic vocabulary.</summary>
        public static List<KitItem> Equipped(Inventory inventory)
        {
            var list = new List<KitItem>(DollOrder.Length);
            for (int i = 0; i < DollOrder.Length; i++)
            {
                var item = inventory.Equipped(DollOrder[i]);
                if (item != null) list.Add(new KitItem(item.Name, SlotNames[i], item.PowerScore, Effects(item)));
            }
            return list;
        }

        /// <summary>The pack, in pack order — the index IS the index Inventory.EquipFromPack wants.</summary>
        public static List<KitItem> Pack(Inventory inventory)
        {
            var list = new List<KitItem>(inventory.Pack.Count);
            for (int i = 0; i < inventory.Pack.Count; i++)
            {
                var item = inventory.Pack[i];
                list.Add(new KitItem(item.Name, NameOf(item.Slot), item.PowerScore, Effects(item)));
            }
            return list;
        }

        /// <summary>
        /// What a piece of gear does, in plain lines. `ItemRules.Describe` already wrote these and
        /// nothing was showing them; all this does is drop the affix's nickname, because "Plated"
        /// is flavour and "+3 armour" is the fact the player is choosing between.
        /// </summary>
        private static List<string> Effects(ItemInstance item)
        {
            var lines = new List<string>(item.Affixes.Count);
            for (int i = 0; i < item.Affixes.Count; i++)
            {
                string described = ItemRules.Describe(item.Affixes[i]);
                int split = described.IndexOf("  ", StringComparison.Ordinal);
                lines.Add(split > 0 ? described.Substring(split + 2).Trim() : described);
            }
            return lines;
        }

        /// <summary>The player-facing name of a slot. Falls back to the enum, which should never show.</summary>
        public static string NameOf(Slot slot)
        {
            for (int i = 0; i < DollOrder.Length; i++)
                if (DollOrder[i] == slot) return SlotNames[i];
            return slot.ToString();
        }

        private KitPageLayout BuildPage()
            => new KitPageLayout(SlotNames, Equipped(_loadout.Inventory), Pack(_loadout.Inventory));

        /// <summary>Rebuild from the model, keeping the cursor where the player left it.</summary>
        public void Rebuild()
        {
            var zone = _page.Zone;
            int row = _page.Row;
            _page = BuildPage();
            _page.SetCursor(zone, row);
        }

        // ------------------------------------------------------------------ input

        /// <summary>Reads the devices and applies a frame. Returns true while this screen owns input.</summary>
        public bool HandleInput()
        {
            if (!IsOpen) return false;
            Apply(_input.Read());
            return true;
        }

        /// <summary>
        /// One frame of navigation, decided. Pure: no UnityEngine, no devices, no time — which is
        /// what lets every rule on this screen be asserted in EditMode.
        /// </summary>
        public KitScreenAction Apply(ComicNav nav)
        {
            if (!IsOpen) return KitScreenAction.None;

            if (nav.Cancel)
            {
                Hide();
                _host.Tick();
                return KitScreenAction.Closed;
            }

            if (nav.Moved)
            {
                if (nav.Up) _page.MoveUp();
                if (nav.Down) _page.MoveDown();
                if (nav.Left) _page.MoveLeft();
                if (nav.Right) _page.MoveRight();
                _host.Tick();
            }

            if (nav.Secondary) return Scrap();
            if (nav.Confirm) return Act();
            return KitScreenAction.None;
        }

        private KitScreenAction Act()
        {
            var action = _page.Confirm();
            switch (action)
            {
                case KitAction.Equip:
                {
                    int index = _page.SelectedPackIndex;
                    if (index < 0 || index >= _loadout.Inventory.Pack.Count) return Refuse("nothing there");

                    var item = _loadout.Inventory.Pack[index];
                    float delta = _loadout.Inventory.UpgradeDelta(item);
                    if (!_loadout.EquipFromPack(index)) return Refuse("could not put that on");

                    Changed();
                    _host.Accept();
                    _host.Notice($"wearing {item.Name} — {(delta >= 0f ? "+" : "")}{delta:F1} weight in {NameOf(item.Slot)}");
                    return KitScreenAction.Equipped;
                }

                case KitAction.Unequip:
                {
                    int index = _page.SelectedSlotIndex;
                    if (index < 0 || index >= DollOrder.Length) return Refuse("nothing there");

                    var slot = DollOrder[index];
                    var item = _loadout.Inventory.Equipped(slot);
                    if (item == null) return Refuse("that slot is already empty");
                    // A full pack is the only way this fails, and it is worth saying out loud:
                    // a silent refusal here reads as the button not working.
                    if (!_loadout.Unequip(slot)) return Refuse("pack is full — scrap something first");

                    Changed();
                    _host.Accept();
                    _host.Notice($"stowed {item.Name} in the pack");
                    return KitScreenAction.Unequipped;
                }

                default:
                    // Standing on an empty slot. Say why nothing happened rather than eating the press.
                    return Refuse("that slot is empty — take something out of the pack");
            }
        }

        /// <summary>X on a pack item: turn it into scrip. The pack is storage, not a museum.</summary>
        private KitScreenAction Scrap()
        {
            int index = _page.SelectedPackIndex;
            if (index < 0 || index >= _loadout.Inventory.Pack.Count)
                return Refuse("nothing in the pack under the cursor");

            var item = _loadout.Inventory.Pack[index];
            int paid = _loadout.Inventory.SellFromPack(index);
            Changed();
            _host.Accept();
            _host.Notice($"scrapped {item.Name} for {paid} scrip");
            return KitScreenAction.Scrapped;
        }

        private KitScreenAction Refuse(string why)
        {
            _host.Refuse();
            _host.Notice(why);
            return KitScreenAction.Refused;
        }

        private void Changed()
        {
            Rebuild();
            _changed?.Invoke();
        }

        // ------------------------------------------------------------------ copy

        /// <summary>
        /// The control line, named for the device in the player's hands. Never assemble a prompt
        /// anywhere else: ADR-002 makes the pad the design centre and the last version of this screen
        /// printed keyboard keys at a player holding a controller.
        /// </summary>
        public static string PromptLine(bool pad, bool onPack) => ComicPrompts.Join(
            $"{ComicPrompts.Move(pad)} — move",
            onPack ? $"{ComicPrompts.Confirm(pad)} — put it on" : $"{ComicPrompts.Confirm(pad)} — take it off",
            onPack ? $"{ComicPrompts.Secondary(pad)} — scrap for scrip" : "",
            $"{ComicPrompts.Cancel(pad)} — back to the fight");

        /// <summary>The heading, with the scrip balance so the scrap prompt means something.</summary>
        public string Subtitle => $"{Title}     ·     {_loadout.Inventory.Scrip} scrip";

        // ------------------------------------------------------------------ drawing

        /// <summary>Draws the whole screen. Call inside an OnGUI that has already done BeginScaledUi.</summary>
        public void Draw(float uiW, float uiH) => Draw(uiW, uiH, ComicInput.PadPresent);

        /// <summary>Same, with the device forced — the screenshot harness uses it to photograph both.</summary>
        public void Draw(float uiW, float uiH, bool pad)
        {
            if (!IsOpen) return;

            ComicScreenChrome.Backdrop(uiW, uiH);
            _page.Layout(ComicScreenChrome.PageRect(uiW, uiH));
            ComicPages.DrawKit(_page, Subtitle);
            ComicScreenChrome.Footer(ComicScreenChrome.FooterRect(uiW, uiH), Explain,
                                     PromptLine(pad, _page.Zone == KitZone.Pack));
        }
    }
}
