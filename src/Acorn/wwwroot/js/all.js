document.addEventListener('DOMContentLoaded', () => {
  const sidebar = document.querySelector("body > .sidebar");
  const sidebarHideButton = document.getElementById("sidebarHide");
  const sidebarShowButton = document.getElementById("sidebarShow");

  const setSidebarVisible = (visible) => {
    sidebar.classList.toggle("hidden", !visible);
    sidebarShowButton.classList.toggle("hidden", visible);
  };

  sidebarHideButton.addEventListener("click", () => setSidebarVisible(false));
  sidebarShowButton.addEventListener("click", () => setSidebarVisible(true));

  setSidebarVisible(window.matchMedia("(min-width: 1280px)").matches);
});
