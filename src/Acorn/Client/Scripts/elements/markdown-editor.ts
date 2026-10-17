type WrapAction = {
  kind: "wrap";
  label: string;
  icon: string;
  prefix: string;
  suffix: string;
  // When set, a file is picked and uploaded first; "{url}" in the suffix receives the result.
  // `attribute` names the host attribute holding the upload endpoint.
  upload?: { attribute: string; accept: string; filenameAsText?: boolean };
};

type LineAction = { kind: "line"; label: string; icon: string; prefix: string };

type MarkdownAction = WrapAction | LineAction;

// Heroicons v2 (24px, outline) path data
const icon = (path: string, linecap = true): string =>
  `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true" style="width: 1.25rem; height: 1.25rem"><path ${linecap ? 'stroke-linecap="round" ' : ""}stroke-linejoin="round" d="${path}"/></svg>`;

const pickFile = (accept: string): Promise<File | undefined> =>
  new Promise((resolve) => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = accept;
    input.addEventListener("change", () => resolve(input.files?.[0]), { once: true });
    input.addEventListener("cancel", () => resolve(undefined), { once: true });
    input.click();
  });

// Accepts a plain-text URL or JSON ({ error } / { id }) response
const uploadFile = async (uploadUrl: string, file: File): Promise<string> => {
  const token = document.querySelector<HTMLInputElement>(
    'input[name="__RequestVerificationToken"]',
  )?.value;
  const formData = new FormData();
  formData.append("file", file);
  if (token) {
    formData.append("__RequestVerificationToken", token);
  }

  const response = await fetch(uploadUrl, {
    method: "POST",
    body: formData,
    credentials: "same-origin",
    headers: token ? { RequestVerificationToken: token } : {},
  });
  const body = (await response.text()).trim();

  let result: unknown = body;
  try {
    result = JSON.parse(body);
  } catch {
    // plain-text response: the body itself is the URL
  }

  if (!response.ok) {
    throw new Error((result as { error?: string } | null)?.error ?? "The upload failed.");
  }
  if (typeof result === "string" && result) {
    return result;
  }
  const id = (result as { id?: string } | null)?.id;
  if (id) {
    return `file:${id}`;
  }
  throw new Error("The upload response did not contain a URL.");
};

const H_BASE = "M2.243 4.493v7.5m0 0v7.502m0-7.501h10.5m0-7.5v7.5m0 0v7.501";

// Each inner array is a visually separated group of toolbar buttons
const actions: MarkdownAction[][] = [
  [
    {
      kind: "wrap",
      label: "Bold",
      icon: icon(
        "M6.75 3.744h-.753v8.25h7.125a4.125 4.125 0 0 0 0-8.25H6.75Zm0 0v.38m0 16.122h6.747a4.5 4.5 0 0 0 0-9.001h-7.5v9h.753Zm0 0v-.37m0-15.751h6a3.75 3.75 0 1 1 0 7.5h-6m0-7.5v7.5m0 0v8.25m0-8.25h6.375a4.125 4.125 0 0 1 0 8.25H6.75m.747-15.38h4.875a3.375 3.375 0 0 1 0 6.75H7.497v-6.75Zm0 7.5h5.25a3.75 3.75 0 0 1 0 7.5h-5.25v-7.5Z",
        false,
      ),
      prefix: "**",
      suffix: "**",
    },
    {
      kind: "wrap",
      label: "Italic",
      icon: icon("M5.248 20.246H9.05m0 0h3.696m-3.696 0 5.893-16.502m0 0h-3.697m3.697 0h3.803"),
      prefix: "_",
      suffix: "_",
    },
    {
      kind: "wrap",
      label: "Strikethrough",
      icon: icon(
        "M12 12a8.912 8.912 0 0 1-.318-.079c-1.585-.424-2.904-1.247-3.76-2.236-.873-1.009-1.265-2.19-.968-3.301.59-2.2 3.663-3.29 6.863-2.432A8.186 8.186 0 0 1 16.5 5.21M6.42 17.81c.857.99 2.176 1.812 3.761 2.237 3.2.858 6.274-.23 6.863-2.431.233-.868.044-1.779-.465-2.617M3.75 12h16.5",
      ),
      prefix: "~~",
      suffix: "~~",
    },
  ],
  [
    {
      kind: "line",
      label: "Heading 1",
      icon: icon(`${H_BASE}m4.501-8.627 2.25-1.5v10.126m0 0h-2.25m2.25 0h2.25`),
      prefix: "# ",
    },
    {
      kind: "line",
      label: "Heading 2",
      icon: icon(
        `${H_BASE}M21.75 19.5H16.5v-1.609a2.25 2.25 0 0 1 1.244-2.012l2.89-1.445c.651-.326 1.116-.955 1.116-1.683 0-.498-.04-.987-.118-1.463-.135-.825-.835-1.422-1.668-1.489a15.202 15.202 0 0 0-3.464.12`,
      ),
      prefix: "## ",
    },
    {
      kind: "line",
      label: "Heading 3",
      icon: icon(
        `${H_BASE}M20.905 14.626a4.52 4.52 0 0 1 .738 3.603c-.154.695-.794 1.143-1.504 1.208a15.194 15.194 0 0 1-3.639-.104m4.405-4.707a4.52 4.52 0 0 0 .738-3.603c-.154-.696-.794-1.144-1.504-1.209a15.19 15.19 0 0 0-3.639.104m4.405 4.708H18`,
      ),
      prefix: "### ",
    },
  ],
  [
    {
      kind: "line",
      label: "Bulleted list",
      icon: icon(
        "M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0ZM3.75 12h.007v.008H3.75V12Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm-.375 5.25h.007v.008H3.75v-.008Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z",
      ),
      prefix: "- ",
    },
    {
      kind: "line",
      label: "Numbered list",
      icon: icon(
        "M8.242 5.992h12m-12 6.003H20.24m-12 5.999h12M4.117 7.495v-3.75H2.99m1.125 3.75H2.99m1.125 0H5.24m-1.92 2.577a1.125 1.125 0 1 1 1.591 1.59l-1.83 1.83h2.16M2.99 15.745h1.125a1.125 1.125 0 0 1 0 2.25H3.74m0-.002h.375a1.125 1.125 0 0 1 0 2.25H2.99",
      ),
      prefix: "1. ",
    },
  ],
  [
    {
      kind: "line",
      label: "Quote",
      icon: icon(
        "M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 0 1 .865-.501 48.172 48.172 0 0 0 3.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z",
      ),
      prefix: "> ",
    },
    {
      kind: "wrap",
      label: "Inline code",
      icon: icon("M17.25 6.75 22.5 12l-5.25 5.25m-10.5 0L1.5 12l5.25-5.25m7.5-3-4.5 16.5"),
      prefix: "`",
      suffix: "`",
    },
    {
      kind: "wrap",
      label: "Code block",
      icon: icon(
        "M14.25 9.75 16.5 12l-2.25 2.25m-4.5 0L7.5 12l2.25-2.25M6 20.25h12A2.25 2.25 0 0 0 20.25 18V6A2.25 2.25 0 0 0 18 3.75H6A2.25 2.25 0 0 0 3.75 6v12A2.25 2.25 0 0 0 6 20.25Z",
      ),
      prefix: "```\n",
      suffix: "\n```",
    },
  ],
  [
    {
      kind: "wrap",
      label: "Link",
      icon: icon(
        "M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244",
      ),
      prefix: "[",
      suffix: "](url)",
    },
    {
      kind: "wrap",
      label: "Image",
      icon: icon(
        "m2.25 15.75 5.159-5.159a2.25 2.25 0 0 1 3.182 0l5.159 5.159m-1.5-1.5 1.409-1.409a2.25 2.25 0 0 1 3.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 0 0 1.5-1.5V6a1.5 1.5 0 0 0-1.5-1.5H3.75A1.5 1.5 0 0 0 2.25 6v12a1.5 1.5 0 0 0 1.5 1.5Zm10.5-11.25h.008v.008h-.008V8.25Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z",
      ),
      prefix: "![",
      suffix: "]({url})",
      upload: { attribute: "image-upload-url", accept: "image/*" },
    },
    {
      kind: "wrap",
      label: "Attach file",
      icon: icon(
        "M18.375 12.739l-7.693 7.693a4.5 4.5 0 0 1-6.364-6.364l10.94-10.94a3 3 0 0 1 4.243 4.243L8.56 18.313a1.5 1.5 0 0 1-2.122-2.122l8.485-8.485",
      ),
      prefix: "[",
      suffix: "]({url})",
      upload: { attribute: "file-upload-url", accept: "*/*", filenameAsText: true },
    },
  ],
];

class MarkdownEditor extends HTMLElement {
  connectedCallback(): void {
    if (this.querySelector("textarea")) {
      return;
    }

    // textContent preserves newlines; innerText collapses them under white-space: normal
    const initialContent = this.textContent ?? "";

    const toolbar = document.createElement("div");
    toolbar.className = "toolbar";
    toolbar.setAttribute("role", "toolbar");
    toolbar.setAttribute("aria-label", "Markdown formatting");

    const textarea = document.createElement("textarea");
    textarea.value = initialContent;
    textarea.setAttribute("aria-label", "Markdown content");
    const name = this.getAttribute("name");
    if (name) {
      textarea.name = name;
    }
    // Move the id to the textarea so <label for> targets it and ids stay unique
    if (this.id) {
      textarea.id = this.id;
      this.removeAttribute("id");
    }
    textarea.rows = Number(this.getAttribute("rows")) || 12;

    actions.forEach((group) => {
      const groupElement = document.createElement("div");
      groupElement.setAttribute("role", "group");
      group.forEach((action) => {
        const button = document.createElement("button");
        button.type = "button";
        button.innerHTML = action.icon;
        button.title = action.label;
        button.setAttribute("aria-label", action.label);
        button.addEventListener("mousedown", (event) => event.preventDefault());
        button.addEventListener("click", () => this.applyAction(textarea, action));
        groupElement.append(button);
      });
      toolbar.append(groupElement);
    });

    this.textContent = "";
    this.append(toolbar, textarea);
  }

  private async applyAction(textarea: HTMLTextAreaElement, action: MarkdownAction): Promise<void> {
    if (textarea.disabled) {
      return;
    }

    if (action.kind === "wrap") {
      await this.applyWrap(textarea, action);
    } else {
      this.applyLine(textarea, action);
    }
  }

  private async applyWrap(textarea: HTMLTextAreaElement, action: WrapAction): Promise<void> {
    const uploadUrl = action.upload ? this.getAttribute(action.upload.attribute) : null;
    let file: File | undefined;
    if (action.upload) {
      if (!uploadUrl) {
        return;
      }
      file = await pickFile(action.upload.accept);
      if (!file) {
        return;
      }
    }

    const start = textarea.selectionStart;
    const selectedText = textarea.value.slice(start, textarea.selectionEnd);
    const linkText = selectedText || (action.upload?.filenameAsText && file
      ? file.name.replace(/\\/g, "\\\\").replace(/\[/g, "\\[").replace(/\]/g, "\\]")
      : "");
    const render = (url: string): string =>
      action.prefix + linkText + action.suffix.replace("{url}", url);
    const innerStart = start + action.prefix.length;
    const selectInner = () =>
      textarea.setSelectionRange(innerStart, innerStart + linkText.length);

    const initial = render("Uploading...");
    textarea.focus();
    this.replaceRange(textarea, start, start + selectedText.length, initial);
    selectInner();

    if (!file || !uploadUrl) {
      return;
    }

    // Lock the textarea so the placeholder range stays valid until the upload settles
    textarea.disabled = true;
    let final = selectedText;
    let succeeded = false;
    try {
      final = render(await uploadFile(uploadUrl, file));
      succeeded = true;
    } catch (error) {
      window.alert(error instanceof Error ? error.message : "The upload failed.");
    } finally {
      textarea.disabled = false;
      textarea.focus();
      this.replaceRange(textarea, start, start + initial.length, final);
      if (succeeded) {
        selectInner();
      }
    }
  }

  private applyLine(textarea: HTMLTextAreaElement, action: LineAction): void {
    const value = textarea.value;
    const selectionStart = textarea.selectionStart;
    const selectionEnd = textarea.selectionEnd;

    textarea.focus();

    const lineStart = value.lastIndexOf("\n", selectionStart - 1) + 1;
    const selectionEndLineBreak = value.indexOf("\n", selectionEnd);
    const lineEnd = selectionEndLineBreak === -1 ? value.length : selectionEndLineBreak;
    const prefixedLines = value
      .slice(lineStart, lineEnd)
      .split("\n")
      .map((line) => action.prefix + line)
      .join("\n");

    this.replaceRange(textarea, lineStart, lineEnd, prefixedLines);
    if (selectionStart === selectionEnd) {
      const cursor = selectionStart + action.prefix.length;
      textarea.setSelectionRange(cursor, cursor);
    } else {
      textarea.setSelectionRange(lineStart, lineStart + prefixedLines.length);
    }
  }

  // execCommand keeps the edit on the browser's undo stack; setting .value does not
  private replaceRange(
    textarea: HTMLTextAreaElement,
    start: number,
    end: number,
    text: string,
  ): void {
    textarea.setSelectionRange(start, end);
    if (!document.execCommand("insertText", false, text)) {
      textarea.setRangeText(text, start, end, "end");
    }
  }
}

if (!customElements.get("markdown-editor")) {
  customElements.define("markdown-editor", MarkdownEditor);
}