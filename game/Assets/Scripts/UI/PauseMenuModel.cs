#nullable enable
namespace Cipher.Game.UI
{
    public enum PauseMenuAction { None, Resume, Quit }

    /// <summary>
    /// Pure state for the pause menu — no UnityEngine so it is unit-testable.
    /// Rendering and input live in the bootstrap; this owns "what is selected"
    /// and "what happens on confirm".
    /// </summary>
    public sealed class PauseMenuModel
    {
        public static readonly string[] Items = { "Resume", "Quit" };

        public bool IsOpen { get; private set; }
        public int SelectedIndex { get; private set; }

        public string SelectedLabel => Items[SelectedIndex];

        /// <summary>Start / Esc: open the menu, or close it if already open.</summary>
        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            IsOpen = true;
            SelectedIndex = 0; // always land on Resume so a reflex double-tap never quits
        }

        public void Close() => IsOpen = false;

        public void MoveUp()
        {
            if (!IsOpen) return;
            SelectedIndex = (SelectedIndex - 1 + Items.Length) % Items.Length;
        }

        public void MoveDown()
        {
            if (!IsOpen) return;
            SelectedIndex = (SelectedIndex + 1) % Items.Length;
        }

        /// <summary>A / Enter / click on the selected item.</summary>
        public PauseMenuAction Confirm()
        {
            if (!IsOpen) return PauseMenuAction.None;
            return Select(SelectedIndex);
        }

        /// <summary>Direct pick (mouse click on a specific row).</summary>
        public PauseMenuAction Select(int index)
        {
            if (!IsOpen || index < 0 || index >= Items.Length) return PauseMenuAction.None;
            SelectedIndex = index;
            if (index == 0) { Close(); return PauseMenuAction.Resume; }
            return PauseMenuAction.Quit;
        }

        /// <summary>B / Esc while open: back out to the game.</summary>
        public PauseMenuAction Cancel()
        {
            if (!IsOpen) return PauseMenuAction.None;
            Close();
            return PauseMenuAction.Resume;
        }
    }
}
