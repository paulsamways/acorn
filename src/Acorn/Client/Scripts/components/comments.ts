document.addEventListener("DOMContentLoaded", () => {
  const wrapper = document.getElementById("comment-form-wrapper");
  const parentInput = wrapper?.querySelector<HTMLInputElement>("input[name='Input.ParentCommentId']");

  const leaveComment = document.getElementById("leave-comment");
  const cancel = document.getElementById("comment-reply-cancel");
  const home = document.getElementById("comment-form-home");

  if (!wrapper || !parentInput || !leaveComment || !cancel || !home) {
    return;
  }

  const removeFromSlot = () => {
    parentInput.value = "";
    const slot = wrapper.closest(".comment-reply-slot");
    if (slot) {
      slot.removeChild(wrapper);
      const article = slot.closest("article");
      if (article) {
        article.classList.remove("replying-to");
      }
    }
  };

  const hideCommentForm = () => {
    removeFromSlot();

    wrapper.hidden = true;
    leaveComment.hidden = false;
    home.appendChild(wrapper);
  };

  const showCommentForm = (parentCommentId?: string) => {
    removeFromSlot();

    if (parentCommentId != null) {
      const article = document.querySelector<HTMLElement>(`article[data-comment="${CSS.escape(parentCommentId)}"]`);
      const slot = article?.querySelector<HTMLElement>('.comment-reply-slot');
      if (article && slot)
      {
        article.classList.add("replying-to");
        slot.appendChild(wrapper);

        parentInput.value = parentCommentId;
      }
    }

    wrapper.hidden = false;
    wrapper.scrollIntoView({ block: "center", behavior: "smooth" });
    // Skips hidden fields and the honeypot (tabindex -1)
    const fields = wrapper.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
      "input:not([type=hidden]):not([tabindex='-1']), textarea",
    );
    Array.from(fields).find((field) => field.value === "")?.focus();
    leaveComment.hidden = true;
  };

  document.querySelectorAll<HTMLLinkElement>("a[data-reply-to]").forEach((link) => {
    link.addEventListener("click", (e) => {
      e.preventDefault();
      showCommentForm(link.dataset['replyTo'])
    });
  });
  leaveComment?.addEventListener("click", () => showCommentForm());
  cancel.addEventListener("click", hideCommentForm);

  // A form re-rendered after a validation error keeps its parent id; restore the reply context.
  if (parentInput.value) {
    showCommentForm(parentInput.value);
  }
});
