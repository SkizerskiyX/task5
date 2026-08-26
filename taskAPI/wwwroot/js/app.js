import { mountTrailerPlayer } from "./trailer-player.js";

const elements = {
    locale: document.querySelector("#locale"),
    seed: document.querySelector("#seed"),
    randomSeed: document.querySelector("#random-seed"),
    advancedToggle: document.querySelector("#advanced-toggle"),
    advancedPanel: document.querySelector("#advanced-panel"),
    applySeed: document.querySelector("#apply-seed"),
    likes: document.querySelector("#likes"),
    reviews: document.querySelector("#reviews"),
    likesValue: document.querySelector("#likes-value"),
    reviewsValue: document.querySelector("#reviews-value"),
    tableView: document.querySelector("#table-view"),
    galleryView: document.querySelector("#gallery-view"),
    tableBody: document.querySelector("#movie-table-body"),
    gallery: document.querySelector("#movie-gallery"),
    gallerySentinel: document.querySelector("#gallery-sentinel"),
    previousPage: document.querySelector("#previous-page"),
    nextPage: document.querySelector("#next-page"),
    pageIndicator: document.querySelector("#page-indicator"),
    loading: document.querySelector("#loading"),
    error: document.querySelector("#error"),
    viewButtons: [...document.querySelectorAll("[data-view]")]
};

const state = {
    configuration: null,
    view: "table",
    seed: null,
    locale: null,
    likes: 0,
    reviews: 0,
    tablePage: 1,
    galleryPage: 1,
    galleryLoading: false,
    requestVersion: 0
};

let changeTimer = null;

async function initialize() {
    try {
        setLoading(true);

        state.configuration = await requestJson("/api/configuration");

        configureControls();

        state.seed = await generateSeed();
        elements.seed.value = state.seed;

        bindEvents();
        createGalleryObserver();

        await loadCurrentView();
    } catch (error) {
        showError(error);
    } finally {
        setLoading(false);
    }
}

function configureControls() {
    const configuration = state.configuration;

    elements.locale.replaceChildren();

    for (const locale of configuration.locales) {
        const option = document.createElement("option");
        option.value = locale;
        option.textContent = locale;
        elements.locale.append(option);
    }

    elements.locale.value = configuration.defaultLocale;

    elements.likes.min = configuration.minimumAverage;
    elements.likes.max = configuration.maximumAverage;
    elements.likes.value = configuration.defaultLikesAverage;

    elements.reviews.min = configuration.minimumAverage;
    elements.reviews.max = configuration.maximumAverage;
    elements.reviews.value = configuration.defaultReviewsAverage;

    state.locale = configuration.defaultLocale;
    state.likes = Number(configuration.defaultLikesAverage);
    state.reviews = Number(configuration.defaultReviewsAverage);

    updateAverageOutputs();
}

function bindEvents() {
    elements.locale.addEventListener("change", () => {
        state.locale = elements.locale.value;
        queueParameterChange();
    });

    elements.advancedToggle.addEventListener("click", () => {
        const isOpen =
            !elements.advancedPanel.classList.contains("hidden");

        elements.advancedPanel.classList.toggle(
            "hidden",
            isOpen
        );

        elements.advancedToggle.setAttribute(
            "aria-expanded",
            String(!isOpen)
        );
    });

    elements.applySeed.addEventListener("click", async () => {
        await applyManualSeed();
    });

    elements.seed.addEventListener("keydown", async event => {
        if (event.key !== "Enter") {
            return;
        }

        event.preventDefault();

        await applyManualSeed();
    });

    elements.randomSeed.addEventListener("click", async () => {
        try {
            elements.randomSeed.disabled = true;

            state.seed = await generateSeed();
            elements.seed.value = state.seed;

            await resetAndLoad();
        } catch (error) {
            showError(error);
        } finally {
            elements.randomSeed.disabled = false;
        }
    });

    elements.likes.addEventListener("input", () => {
        state.likes = Number(elements.likes.value);
        updateAverageOutputs();
        queueParameterChange();
    });

    elements.reviews.addEventListener("input", () => {
        state.reviews = Number(elements.reviews.value);
        updateAverageOutputs();
        queueParameterChange();
    });

    elements.previousPage.addEventListener("click", async () => {
        if (state.tablePage <= 1) {
            return;
        }

        state.tablePage -= 1;
        await loadTable();
    });

    elements.nextPage.addEventListener("click", async () => {
        state.tablePage += 1;
        await loadTable();
    });

    for (const button of elements.viewButtons) {
        button.addEventListener("click", async () => {
            const view = button.dataset.view;

            if (!view || view === state.view) {
                return;
            }

            await switchView(view);
        });
    }
}

function queueParameterChange() {
    window.clearTimeout(changeTimer);

    changeTimer = window.setTimeout(
        resetAndLoad,
        getDebounceDelay()
    );
}

function getDebounceDelay() {
    return 250;
}

async function resetAndLoad() {
    state.tablePage = 1;
    state.galleryPage = 1;
    state.requestVersion += 1;

    closeActiveTrailers();

    elements.gallery.replaceChildren();

    await loadCurrentView();
}

async function switchView(view) {
    state.view = view;

    for (const button of elements.viewButtons) {
        button.classList.toggle(
            "active",
            button.dataset.view === view
        );
    }

    elements.tableView.classList.toggle(
        "hidden",
        view !== "table"
    );

    elements.galleryView.classList.toggle(
        "hidden",
        view !== "gallery"
    );

    closeActiveTrailers();

    if (view === "gallery" && elements.gallery.childElementCount === 0) {
        state.galleryPage = 1;
        await loadGalleryPage();
    }

    if (view === "table") {
        await loadTable();
    }
}

async function loadCurrentView() {
    if (state.view === "table") {
        await loadTable();
        return;
    }

    await loadGalleryPage();
}

async function loadTable() {
    const requestVersion = state.requestVersion;

    try {
        setLoading(true);
        hideError();

        const response = await requestMovies(state.tablePage);

        if (requestVersion !== state.requestVersion) {
            return;
        }

        renderTable(response.items);

        elements.pageIndicator.textContent = `Page ${response.page}`;
        elements.previousPage.disabled = response.page <= 1;
    } catch (error) {
        showError(error);
    } finally {
        setLoading(false);
    }
}

async function loadGalleryPage() {
    if (state.galleryLoading || state.view !== "gallery") {
        return;
    }

    const requestVersion = state.requestVersion;
    const page = state.galleryPage;

    state.galleryLoading = true;

    try {
        setLoading(true);
        hideError();

        const response = await requestMovies(page);

        if (requestVersion !== state.requestVersion) {
            return;
        }

        renderGallery(response.items);
        state.galleryPage += 1;
    } catch (error) {
        showError(error);
    } finally {
        state.galleryLoading = false;
        setLoading(false);
    }
}

async function requestMovies(page) {
    const parameters = new URLSearchParams({
        seed: state.seed,
        locale: state.locale,
        page: String(page),
        pageSize: String(state.configuration.defaultPageSize),
        likes: String(state.likes),
        reviews: String(state.reviews)
    });

    return requestJson(`/api/movies?${parameters}`);
}

function renderTable(movies) {
    elements.tableBody.replaceChildren();

    for (const movie of movies) {
        const row = document.createElement("tr");

        row.innerHTML = `
            <td class="movie-index">${escapeHtml(movie.index)}</td>
            <td class="movie-title">${escapeHtml(movie.title)}</td>
            <td>${escapeHtml(movie.year)}</td>
            <td>${escapeHtml(movie.genre)}</td>
            <td class="actor-list">${escapeHtml(movie.actors.join(", "))}</td>
            <td class="metric">${escapeHtml(movie.likes)}</td>
            <td class="metric">${escapeHtml(movie.reviews.length)}</td>
            <td>
                <button
                    class="details-button"
                    type="button">
                    Details
                </button>
            </td>
        `;

        const detailsRow = document.createElement("tr");
        detailsRow.className = "details-row hidden";

        const detailsCell = document.createElement("td");
        detailsCell.colSpan = row.children.length;

        detailsRow.append(detailsCell);

        const button = row.querySelector(".details-button");

        button.addEventListener("click", async () => {
            await toggleTableDetails(
                movie,
                detailsRow,
                detailsCell,
                button
            );
        });

        elements.tableBody.append(row, detailsRow);
    }
}

async function toggleTableDetails(
    movie,
    detailsRow,
    detailsCell,
    button
) {
    const isOpen = !detailsRow.classList.contains("hidden");

    if (isOpen) {
        destroyTrailer(detailsCell);
        detailsRow.classList.add("hidden");
        detailsCell.replaceChildren();
        button.textContent = "Details";
        return;
    }

    detailsRow.classList.remove("hidden");
    button.textContent = "Close";

    await renderMovieDetails(movie, detailsCell);
}

function renderGallery(movies) {
    const fragment = document.createDocumentFragment();

    for (const movie of movies) {
        const card = document.createElement("article");
        card.className = "movie-card";

        card.style.setProperty(
            "--card-accent",
            createCardAccent(movie.index)
        );

        card.innerHTML = `
            <div class="movie-card-visual">
                <span class="movie-card-index">#${escapeHtml(movie.index)}</span>
                <h2 class="movie-card-title">${escapeHtml(movie.title)}</h2>
            </div>

            <div class="movie-card-body">
                <div class="movie-card-meta">
                    <span class="tag">${escapeHtml(movie.year)}</span>
                    <span class="tag">${escapeHtml(movie.genre)}</span>
                </div>

                <p class="movie-card-actors">
                    ${escapeHtml(movie.actors.join(", "))}
                </p>

                <div class="movie-card-footer">
                    <div class="movie-card-stats">
                        <span>♥ ${escapeHtml(movie.likes)}</span>
                        <span>Reviews ${escapeHtml(movie.reviews.length)}</span>
                    </div>

                    <button type="button">
                        Details
                    </button>
                </div>
            </div>
        `;

        const button = card.querySelector("button");

        button.addEventListener("click", async () => {
            await toggleCardDetails(movie, card, button);
        });

        fragment.append(card);
    }

    elements.gallery.append(fragment);
}

async function toggleCardDetails(movie, card, button) {
    const current = card.querySelector(".card-details");

    if (current) {
        destroyTrailer(current);
        current.remove();
        button.textContent = "Details";
        return;
    }

    const details = document.createElement("div");
    details.className = "card-details";

    card.append(details);
    button.textContent = "Close";

    await renderMovieDetails(movie, details);
}

async function renderMovieDetails(movie, container) {
    container.innerHTML = `
        <div class="details-content">
            <section class="trailer-panel">
                <h3>Trailer</h3>
                <div data-role="trailer"></div>
            </section>

            <section class="reviews-panel">
                <h3>Reviews</h3>
                <div data-role="reviews"></div>
            </section>
        </div>
    `;

    renderReviews(
        movie.reviews,
        container.querySelector('[data-role="reviews"]')
    );

    const trailerHost = container.querySelector(
        '[data-role="trailer"]'
    );

    try {
        const trailer = await requestTrailer(movie.index);
        mountTrailerPlayer(trailerHost, trailer);
    } catch (error) {
        trailerHost.textContent = error.message;
    }
}

function renderReviews(reviews, container) {
    if (reviews.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                No reviews generated for this movie.
            </div>
        `;

        return;
    }

    const list = document.createElement("ul");
    list.className = "review-list";

    for (const review of reviews) {
        const item = document.createElement("li");
        item.className = "review-item";
        item.textContent = review;
        list.append(item);
    }

    container.append(list);
}

async function requestTrailer(movieIndex) {
    const parameters = new URLSearchParams({
        seed: state.seed,
        locale: state.locale
    });

    return requestJson(
        `/api/trailers/${movieIndex}?${parameters}`
    );
}

function createGalleryObserver() {
    const observer =
        new IntersectionObserver(
            entries => {
                const entry =
                    entries[0];

                if (
                    entry.isIntersecting &&
                    state.view === "gallery"
                ) {
                    loadGalleryPage();
                }
            },
            {
                rootMargin: "500px"
            }
        );

    observer.observe(
        elements.gallerySentinel
    );
}

function createCardAccent(index) {
    const hue =
        Number(BigInt(index) * 47n % 360n);

    return `hsla(${hue}, 80%, 60%, 0.22)`;
}

function closeActiveTrailers() {
    document
        .querySelectorAll(".trailer-shell")
        .forEach(shell => {
            shell.trailerPlayer?.destroy();
        });
}

function destroyTrailer(container) {
    container
        .querySelectorAll(".trailer-shell")
        .forEach(shell => {
            shell.trailerPlayer?.destroy();
        });
}

function normalizeSeed(value) {
    const normalized =
        value.trim();

    if (!/^\d+$/.test(normalized)) {
        return null;
    }

    try {
        const seed =
            BigInt(normalized);

        const maximum =
            BigInt(
                state.configuration.maximumSeed
            );

        if (
            seed < 0n ||
            seed > maximum
        ) {
            return null;
        }

        return seed.toString();
    } catch {
        return null;
    }
}

async function generateSeed() {
    const value =
        await requestJson(
            "/api/movies/seed"
        );

    return String(value);
}

async function applyManualSeed() {
    const value = normalizeSeed(
        elements.seed.value
    );

    if (value === null) {
        showError(
            new Error("Enter a valid seed.")
        );

        return;
    }

    hideError();

    state.seed = value;
    elements.seed.value = value;

    await resetAndLoad();
}

async function requestJson(url) {
    const response =
        await fetch(url, {
            headers: {
                Accept: "application/json"
            }
        });

    if (!response.ok) {
        const body =
            await response.text();

        throw new Error(
            body ||
            `${response.status} ${response.statusText}`
        );
    }

    return response.json();
}

function updateAverageOutputs() {
    elements.likesValue.value =
        Number(state.likes).toFixed(1);

    elements.reviewsValue.value =
        Number(state.reviews).toFixed(1);
}

function setLoading(value) {
    elements.loading.classList.toggle(
        "hidden",
        !value
    );
}

function showError(error) {
    elements.error.textContent =
        error instanceof Error
            ? error.message
            : String(error);

    elements.error.classList.remove(
        "hidden"
    );
}

function hideError() {
    elements.error.classList.add(
        "hidden"
    );

    elements.error.textContent = "";
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

initialize();