// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const feedbackModalElement = document.querySelector("#feedback-modal");
if (feedbackModalElement && window.bootstrap?.Modal) {
    const notices = [...document.querySelectorAll("#feedback-notices [data-feedback-kind]")];
    const validationErrors = [...document.querySelectorAll(".validation-summary-errors li")];
    const messages = [...notices.map(notice => ({
        kind: notice.dataset.feedbackKind,
        text: notice.textContent.trim()
    })), ...validationErrors.map(error => ({
        kind: "error",
        text: error.textContent.trim()
    }))].filter(message => message.text);

    if (messages.length > 0) {
        const uniqueMessages = [...new Map(messages.map(message => [`${message.kind}:${message.text}`, message])).values()];
        const hasError = uniqueMessages.some(message => message.kind === "error");
        document.querySelector("#feedback-modal-title").textContent = hasError ? "Có lỗi xảy ra" : "Thông báo";
        document.querySelector("#feedback-modal-message").replaceChildren(
            ...uniqueMessages.map(message => {
                const paragraph = document.createElement("p");
                paragraph.className = message.kind === "error" ? "text-danger mb-2" : "mb-2";
                paragraph.textContent = message.text;
                return paragraph;
            })
        );
        window.bootstrap.Modal.getOrCreateInstance(feedbackModalElement).show();
    }
}

const readerSelect = document.querySelector("#reader-select");
if (readerSelect) {
    const readerName = document.querySelector("#reader-name");
    const updateReaderName = () => {
        const option = readerSelect.selectedOptions[0];
        readerName.value = option?.dataset.readerName ?? "";
    };
    readerSelect.addEventListener("change", updateReaderName);
    updateReaderName();
}

const borrowRows = document.querySelector("#borrow-book-rows");
if (borrowRows) {
    const template = document.querySelector("#borrow-book-template");
    const addButton = document.querySelector("#add-book-row");

    const updateBookRow = (row) => {
        const select = row.querySelector(".book-select");
        const option = select.selectedOptions[0];
        const meta = row.querySelector("[data-book-meta]");
        const quantity = row.querySelector(".book-quantity");
        quantity.setCustomValidity("");
        if (!option?.dataset.title) {
            meta.textContent = "Chọn sách để xem thông tin còn lại.";
            return;
        }

        meta.textContent = `Mã sách: SH-${option.value} · Tác giả: ${option.dataset.author} · Nhà xuất bản: ${option.dataset.publisher || "—"} · Còn: ${option.dataset.available}`;
        quantity.max = option.dataset.available;
        quantity.setCustomValidity(Number(quantity.value) > Number(option.dataset.available)
            ? `Chỉ còn ${option.dataset.available} cuốn sách này.`
            : "");
    };

    const reindexRows = () => {
        const rows = [...borrowRows.querySelectorAll("[data-book-row]")];
        rows.forEach((row, index) => {
            const select = row.querySelector(".book-select");
            const quantity = row.querySelector(".book-quantity");
            select.name = `Loan.Items[${index}].BookId`;
            select.id = `Loan_Items_${index}__BookId`;
            quantity.name = `Loan.Items[${index}].Quantity`;
            quantity.id = `Loan_Items_${index}__Quantity`;
            row.querySelector("[data-book-label]")?.setAttribute("for", select.id);
            row.querySelector("[data-quantity-label]")?.setAttribute("for", quantity.id);

            const selectedBookId = select.value;
            for (const option of select.options) {
                option.disabled = option.value !== "0" && option.value !== selectedBookId && rows.some(other =>
                    other !== row && other.querySelector(".book-select").value === option.value);
            }

            select.value = selectedBookId;
            updateBookRow(row);
        });
    };

    borrowRows.addEventListener("change", (event) => {
        if (event.target.matches(".book-select")) {
            const selectedBookId = event.target.value;
            const selectedInAnotherRow = [...borrowRows.querySelectorAll(".book-select")]
                .some(select => select !== event.target && select.value === selectedBookId);
            if (selectedBookId !== "0" && selectedInAnotherRow) {
                event.target.value = "0";
            }

            reindexRows();
        }
    });
    borrowRows.addEventListener("input", (event) => {
        if (event.target.matches(".book-quantity")) {
            const row = event.target.closest("[data-book-row]");
            const selected = row.querySelector(".book-select").selectedOptions[0];
            const max = Number(selected?.dataset.available ?? 0);
            event.target.setCustomValidity(Number(event.target.value) > max
                ? `Chỉ còn ${max} cuốn sách này.`
                : "");
        }
    });
    borrowRows.addEventListener("click", (event) => {
        const removeButton = event.target.closest(".remove-book-row");
        if (!removeButton) {
            return;
        }

        const rows = borrowRows.querySelectorAll("[data-book-row]");
        if (rows.length === 1) {
            rows[0].querySelector(".book-select").value = "0";
            rows[0].querySelector(".book-quantity").value = "1";
        } else {
            removeButton.closest("[data-book-row]").remove();
        }

        reindexRows();
    });
    addButton.addEventListener("click", () => {
        borrowRows.append(template.content.cloneNode(true));
        reindexRows();
    });
    reindexRows();
}
