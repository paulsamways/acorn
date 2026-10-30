import "../elements/markdown-editor.js";

document.addEventListener('DOMContentLoaded', () => {
  const sidebar = document.querySelector("body > .sidebar");
  const sidebarHideButton = document.getElementById("sidebarHide");
  const sidebarShowButton = document.getElementById("sidebarShow");

  if (sidebar != null && sidebarHideButton != null && sidebarShowButton != null)
  {
    const setSidebarVisible = (visible: boolean) => {
      sidebar.classList.toggle("hidden", !visible);
      sidebarShowButton.classList.toggle("hidden", visible);
    };

    sidebarHideButton.addEventListener("click", () => setSidebarVisible(false));
    sidebarShowButton.addEventListener("click", () => setSidebarVisible(true));

    setSidebarVisible(window.matchMedia("(min-width: 1280px)").matches);
  }
});
