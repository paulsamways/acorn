(() => {
  const container = document.getElementById("post-image-upload");
  if (!container) return;

  const fileInput = document.getElementById("post-image-file");
  const altInput = document.getElementById("post-image-alt");
  const uploadButton = document.getElementById("post-image-upload-button");
  const progress = document.getElementById("post-image-upload-progress");
  const status = document.getElementById("post-image-upload-status");
  const body = document.getElementById("Body");

  if (!fileInput || !altInput || !uploadButton || !progress || !status || !body) return;

  uploadButton.addEventListener("click", async () => {
    const file = fileInput.files?.[0];
    if (!file) {
      status.textContent = "Choose an image first.";
      return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const formData = new FormData();
    formData.append("file", file);
    if (token) formData.append("__RequestVerificationToken", token);

    uploadButton.disabled = true;
    fileInput.disabled = true;
    progress.hidden = false;
    status.textContent = "Uploading image…";

    try {
      const response = await fetch(container.dataset.uploadUrl, {
        method: "POST",
        body: formData,
        credentials: "same-origin",
        headers: token ? { RequestVerificationToken: token } : {}
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error ?? "The image could not be uploaded.");

      const escapedAlt = altInput.value.trim().replace(/\s+/g, " ").replace(/[\\\]]/g, "\\$&");
      const reference = `![${escapedAlt}](file:${result.publicId})`;
      const start = body.selectionStart ?? body.value.length;
      const end = body.selectionEnd ?? start;
      const prefix = start > 0 && body.value[start - 1] !== "\n" ? "\n" : "";
      const suffix = end < body.value.length && body.value[end] !== "\n" ? "\n" : "";
      body.setRangeText(`${prefix}${reference}${suffix}`, start, end, "end");
      body.dispatchEvent(new Event("input", { bubbles: true }));
      body.focus();
      fileInput.value = "";
      altInput.value = "";
      status.textContent = "Image uploaded and inserted.";
    } catch (error) {
      status.textContent = error instanceof Error ? error.message : "The image could not be uploaded.";
    } finally {
      uploadButton.disabled = false;
      fileInput.disabled = false;
      progress.hidden = true;
    }
  });
})();
