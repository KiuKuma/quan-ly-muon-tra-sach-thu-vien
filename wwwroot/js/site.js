// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelectorAll("[data-password-toggle]").forEach(toggle => {
    const input = document.getElementById(toggle.dataset.passwordToggle);
    if (!input) {
        return;
    }

    toggle.addEventListener("click", () => {
        const isVisible = input.type === "text";
        input.type = isVisible ? "password" : "text";
        toggle.setAttribute("aria-pressed", String(!isVisible));
        toggle.dataset.visible = String(!isVisible);
        toggle.setAttribute(
            "aria-label",
            isVisible
                ? (input.id === "ConfirmPassword" ? "Hiện mật khẩu xác nhận" : "Hiện mật khẩu")
                : (input.id === "ConfirmPassword" ? "Ẩn mật khẩu xác nhận" : "Ẩn mật khẩu")
        );
    });
});

const staffPassword = document.querySelector("#Password");
const staffPasswordConfirmation = document.querySelector("#ConfirmPassword");
if (staffPassword && staffPasswordConfirmation) {
    const passwordGuidance = document.querySelector("#password-guidance");
    const confirmationGuidance = document.querySelector("#confirm-password-guidance");
    const setPasswordStatus = (input, messageElement, valid, message) => {
        input.classList.toggle("is-valid", valid);
        input.classList.toggle("is-invalid", !valid);
        input.setAttribute("aria-invalid", String(!valid));
        messageElement.classList.toggle("text-success", valid);
        messageElement.classList.toggle("text-danger", !valid);
        messageElement.textContent = message;
    };

    const updatePasswordGuidance = () => {
        const password = staffPassword.value;
        const confirmation = staffPasswordConfirmation.value;
        const hasPassword = password.length > 0;
        const isLongEnough = password.length >= Number(staffPassword.minLength || 8);

        if (hasPassword) {
            setPasswordStatus(
                staffPassword,
                passwordGuidance,
                isLongEnough,
                isLongEnough
                    ? "✓ Mật khẩu đã đủ 8 ký tự."
                    : `⚠ Mật khẩu còn thiếu ${8 - password.length} ký tự.`
            );
        } else {
            staffPassword.classList.remove("is-valid", "is-invalid");
            staffPassword.removeAttribute("aria-invalid");
            passwordGuidance.classList.remove("text-success", "text-danger");
            passwordGuidance.textContent = "";
        }

        if (confirmation.length > 0) {
            const matches = hasPassword && confirmation === password;
            setPasswordStatus(
                staffPasswordConfirmation,
                confirmationGuidance,
                matches,
                matches ? "✓ Mật khẩu khớp." : "⚠ Mật khẩu không khớp."
            );
        } else {
            staffPasswordConfirmation.classList.remove("is-valid", "is-invalid");
            staffPasswordConfirmation.removeAttribute("aria-invalid");
            confirmationGuidance.classList.remove("text-success", "text-danger");
            confirmationGuidance.textContent = "";
        }
    };

    staffPassword.addEventListener("input", updatePasswordGuidance);
    staffPasswordConfirmation.addEventListener("input", updatePasswordGuidance);
}

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
    const bookOptions = JSON.parse(document.querySelector("#borrow-book-options").textContent);
    const normalizeBookSearch = value => value
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/[đĐ]/g, "d")
        .toLocaleLowerCase()
        .trim();

    const updateBookRow = (row) => {
        const select = row.querySelector(".book-select");
        const search = row.querySelector(".book-search");
        const meta = row.querySelector("[data-book-meta]");
        const quantity = row.querySelector(".book-quantity");
        const book = bookOptions.find(option => String(option.Id) === select.value);
        search.setCustomValidity(book ? "" : "Vui lòng tìm và chọn sách từ danh sách gợi ý.");
        quantity.max = book?.AvailableCopies ?? 100;
        quantity.setCustomValidity("");
        if (!book) {
            meta.textContent = "Nhập tên hoặc mã sách rồi chọn một kết quả.";
            return;
        }

        meta.textContent = `Mã sách: SH-${book.Id} · Tác giả: ${book.Author} · Nhà xuất bản: ${book.Publisher || "—"} · Còn: ${book.AvailableCopies}`;
        quantity.max = book.AvailableCopies;
        quantity.setCustomValidity(Number(quantity.value) > book.AvailableCopies
            ? `Chỉ còn ${book.AvailableCopies} cuốn sách này.`
            : "");
    };

    const hideSuggestions = row => {
        const list = row.querySelector(".book-suggestions");
        list.hidden = true;
        row.querySelector(".book-search").setAttribute("aria-expanded", "false");
    };

    const showSuggestions = (row, query) => {
        const list = row.querySelector(".book-suggestions");
        const search = normalizeBookSearch(query);
        list.replaceChildren();
        if (!search) {
            hideSuggestions(row);
            return;
        }

        const selectedElsewhere = new Set([...borrowRows.querySelectorAll(".book-select")]
            .filter(input => input !== row.querySelector(".book-select"))
            .map(input => input.value));
        const matches = bookOptions.filter(book => {
            const title = normalizeBookSearch(book.Name);
            const code = normalizeBookSearch(`SH-${book.Id}`);
            return !selectedElsewhere.has(String(book.Id)) &&
                (title.startsWith(search) || code.startsWith(search));
        });

        if (matches.length === 0) {
            const empty = document.createElement("li");
            empty.className = "book-suggestion-empty";
            empty.textContent = "Không tìm thấy sách phù hợp còn bản.";
            list.append(empty);
        } else {
            for (const book of matches) {
                const item = document.createElement("li");
                const button = document.createElement("button");
                button.type = "button";
                button.className = "book-suggestion";
                button.setAttribute("role", "option");
                button.textContent = `SH-${book.Id} · ${book.Name} · Còn ${book.AvailableCopies}`;
                button.addEventListener("click", () => {
                    const select = row.querySelector(".book-select");
                    select.value = String(book.Id);
                    row.querySelector(".book-search").value = book.Name;
                    hideSuggestions(row);
                    reindexRows();
                });
                item.append(button);
                list.append(item);
            }
        }

        list.hidden = false;
        row.querySelector(".book-search").setAttribute("aria-expanded", "true");
    };

    const reindexRows = () => {
        const rows = [...borrowRows.querySelectorAll("[data-book-row]")];
        rows.forEach((row, index) => {
            const select = row.querySelector(".book-select");
            const search = row.querySelector(".book-search");
            const quantity = row.querySelector(".book-quantity");
            select.name = `Loan.Items[${index}].BookId`;
            select.id = `Loan_Items_${index}__BookId`;
            search.id = `book-search-${index}`;
            quantity.name = `Loan.Items[${index}].Quantity`;
            quantity.id = `Loan_Items_${index}__Quantity`;
            row.querySelector("[data-book-label]")?.setAttribute("for", search.id);
            row.querySelector("[data-quantity-label]")?.setAttribute("for", quantity.id);

            const selectedBook = bookOptions.find(book => String(book.Id) === select.value);
            if (selectedBook && !search.value) {
                search.value = selectedBook.Name;
            }
            updateBookRow(row);
        });
    };

    borrowRows.addEventListener("input", event => {
        if (event.target.matches(".book-search")) {
            const row = event.target.closest("[data-book-row]");
            row.querySelector(".book-select").value = "0";
            updateBookRow(row);
            showSuggestions(row, event.target.value);
        }
    });
    borrowRows.addEventListener("focusin", event => {
        if (event.target.matches(".book-search") && event.target.value) {
            showSuggestions(event.target.closest("[data-book-row]"), event.target.value);
        }
    });
    borrowRows.addEventListener("focusout", event => {
        if (event.target.matches(".book-search")) {
            const row = event.target.closest("[data-book-row]");
            window.setTimeout(() => hideSuggestions(row), 120);
        }
    });
    borrowRows.addEventListener("keydown", event => {
        if (!event.target.matches(".book-search")) {
            return;
        }

        const row = event.target.closest("[data-book-row]");
        const firstSuggestion = row.querySelector(".book-suggestion");
        if (event.key === "ArrowDown" && firstSuggestion) {
            event.preventDefault();
            firstSuggestion.focus();
        } else if (event.key === "Enter" && firstSuggestion) {
            event.preventDefault();
            firstSuggestion.click();
        } else if (event.key === "Escape") {
            hideSuggestions(row);
        }
    });
    borrowRows.addEventListener("input", (event) => {
        if (event.target.matches(".book-quantity")) {
            const row = event.target.closest("[data-book-row]");
            const selected = bookOptions.find(book =>
                String(book.Id) === row.querySelector(".book-select").value);
            const max = selected?.AvailableCopies ?? 0;
            event.target.setCustomValidity(selected && Number(event.target.value) > max
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
            const row = rows[0];
            row.querySelector(".book-select").value = "0";
            row.querySelector(".book-search").value = "";
            row.querySelector(".book-quantity").value = "1";
            hideSuggestions(row);
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
