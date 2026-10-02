(() => {
  const form = document.querySelector("[data-bookmark-metadata-url]");
  if (!form) return;

  const urlInput = form.querySelector('[name="Url"]');
  const titleInput = form.querySelector('[name="Title"]');
  const descriptionInput = form.querySelector('[name="Description"]');
  const status = form.querySelector("[data-bookmark-metadata-status]");
  const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
  let timeoutId;
  let activeRequest;

  urlInput?.addEventListener("input", () => {
    window.clearTimeout(timeoutId);
    activeRequest?.abort();

    const url = urlInput.value.trim();
    if (!url) {
      status.textContent = "";
      return;
    }

    timeoutId = window.setTimeout(async () => {
      const controller = new AbortController();
      activeRequest = controller;
      status.textContent = "Checking page...";

      try {
        const response = await fetch(form.dataset.bookmarkMetadataUrl, {
          method: "POST",
          credentials: "same-origin",
          headers: {
            "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
            RequestVerificationToken: token ?? ""
          },
          body: new URLSearchParams({ url }),
          signal: controller.signal
        });

        if (!response.ok) throw new Error("Metadata request failed");

        const result = await response.json();
        if (controller.signal.aborted || urlInput.value.trim() !== url) return;

        if (!result.isValid) {
          status.textContent = result.message ?? "Could not read metadata from this URL.";
          return;
        }

        let populated = false;
        if (result.title && !titleInput.value.trim()) {
          titleInput.value = result.title;
          populated = true;
        }
        if (result.description && !descriptionInput.value.trim()) {
          descriptionInput.value = result.description;
          populated = true;
        }

        status.textContent = populated ? "Page metadata loaded." : "URL is valid.";
      } catch (error) {
        if (!controller.signal.aborted && urlInput.value.trim() === url)
          status.textContent = "Could not read metadata from this URL.";
      }
    }, 650);
  });
})();
