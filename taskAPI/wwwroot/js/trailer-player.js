export function mountTrailerPlayer(host, descriptor) {
    const shell = document.createElement("div");
    shell.className = "trailer-shell";
    shell.innerHTML = `
        <canvas class="trailer-canvas"></canvas>
        <div class="trailer-controls">
            <button type="button" data-action="play">Play</button>
            <button type="button" data-action="stop">Stop</button>
            <span data-role="time"></span>
        </div>
    `;

    host.replaceChildren(shell);

    const canvas = shell.querySelector(".trailer-canvas");
    const playButton = shell.querySelector('[data-action="play"]');
    const stopButton = shell.querySelector('[data-action="stop"]');
    const timeLabel = shell.querySelector('[data-role="time"]');

    canvas.width = descriptor.width;
    canvas.height = descriptor.height;

    const player = new CinematicTrailerPlayer(canvas, descriptor, timeLabel);
    shell.trailerPlayer = player;

    playButton.addEventListener("click", async () => {
        playButton.disabled = true;
        try {
            await player.play();
        } finally {
            playButton.disabled = false;
        }
    });

    stopButton.addEventListener("click", () => player.stop());

    player.initialize().catch(() => player.renderFrame(0));

    return player;
}

class CinematicTrailerPlayer {
    constructor(canvas, descriptor, timeLabel) {
        this.canvas = canvas;
        this.context = canvas.getContext("2d", { alpha: false });
        this.descriptor = descriptor;
        this.timeLabel = timeLabel;
        this.animationFrame = null;
        this.startedAt = 0;
        this.currentSceneIndex = -1;
        this.media = new Map();
        this.audio = null;
        this.prepared = false;
        this.playing = false;
    }

    async initialize() {
        if (this.prepared) {
            return;
        }

        const uniqueScenes = new Map();
        for (const scene of this.descriptor.scenes) {
            uniqueScenes.set(`${scene.assetType}:${scene.asset}`, scene);
        }

        await Promise.all(
            [...uniqueScenes.values()].map(scene => this.loadSceneMedia(scene))
        );

        if (this.descriptor.audio?.asset) {
            this.audio = new Audio();
            this.audio.preload = "auto";
            this.audio.src = this.descriptor.audio.asset;
            this.audio.volume = clamp(this.descriptor.audio.volume, 0, 1);
            this.audio.playbackRate = this.descriptor.audio.playbackRate;
            await waitForAudio(this.audio);
        }

        this.prepared = true;
        this.renderFrame(0);
    }

    async loadSceneMedia(scene) {
        const key = `${scene.assetType}:${scene.asset}`;

        if (scene.assetType === "image") {
            const image = new Image();
            image.decoding = "async";
            image.src = scene.asset;

            try {
                await image.decode();
                this.media.set(key, { type: "image", element: image, ready: true });
            } catch {
                this.media.set(key, { type: "image", element: image, ready: false });
            }

            return;
        }

        const video = document.createElement("video");
        video.preload = "auto";
        video.muted = true;
        video.playsInline = true;
        video.loop = true;
        video.src = scene.asset;

        try {
            await waitForVideo(video);
            this.media.set(key, { type: "video", element: video, ready: true });
        } catch {
            this.media.set(key, { type: "video", element: video, ready: false });
        }
    }

    async play() {
        await this.initialize();
        this.stop(false);
        this.playing = true;
        this.startedAt = performance.now();
        this.currentSceneIndex = -1;

        await this.startAudio();

        const tick = timestamp => {
            if (!this.playing) {
                return;
            }

            const elapsed = (timestamp - this.startedAt) / 1000;

            if (elapsed >= this.descriptor.durationSeconds) {
                this.renderFrame(this.descriptor.durationSeconds);
                this.finishPlayback();
                return;
            }

            this.renderFrame(elapsed);
            this.animationFrame = requestAnimationFrame(tick);
        };

        this.animationFrame = requestAnimationFrame(tick);
    }

    stop(reset = true) {
        this.playing = false;

        if (this.animationFrame !== null) {
            cancelAnimationFrame(this.animationFrame);
            this.animationFrame = null;
        }

        for (const entry of this.media.values()) {
            if (entry.type === "video") {
                entry.element.pause();
            }
        }

        if (this.audio) {
            this.audio.pause();
        }

        this.currentSceneIndex = -1;

        if (reset) {
            this.renderFrame(0);
        }
    }

    destroy() {
        this.stop(false);

        for (const entry of this.media.values()) {
            if (entry.type === "video") {
                entry.element.removeAttribute("src");
                entry.element.load();
            }
        }

        if (this.audio) {
            this.audio.removeAttribute("src");
            this.audio.load();
        }

        this.media.clear();
        this.audio = null;
    }

    finishPlayback() {
        this.playing = false;
        this.animationFrame = null;

        for (const entry of this.media.values()) {
            if (entry.type === "video") {
                entry.element.pause();
            }
        }

        if (this.audio) {
            this.audio.pause();
        }
    }

    async startAudio() {
        if (!this.audio) {
            return;
        }

        const maximumStart = Math.max(0, this.audio.duration - this.descriptor.durationSeconds);
        const requestedStart = this.descriptor.audio.startOffset;
        this.audio.currentTime = Math.min(requestedStart, maximumStart);
        this.audio.volume = clamp(this.descriptor.audio.volume, 0, 1);
        this.audio.playbackRate = this.descriptor.audio.playbackRate;

        try {
            await this.audio.play();
        } catch {
        }
    }

    renderFrame(time) {
        const scene = this.findScene(time) ?? this.descriptor.scenes.at(-1);
        if (!scene) {
            return;
        }

        if (scene.index !== this.currentSceneIndex) {
            this.activateScene(scene);
            this.currentSceneIndex = scene.index;
        }

        const localTime = Math.max(0, time - scene.start);
        const progress = clamp(localTime / scene.duration, 0, 1);

        this.drawScene(scene, progress, time);
        this.timeLabel.textContent = `${Math.min(time, this.descriptor.durationSeconds).toFixed(1)} / ${this.descriptor.durationSeconds.toFixed(1)} s`;
    }

    findScene(time) {
        return this.descriptor.scenes.find(scene =>
            time >= scene.start &&
            time < scene.start + scene.duration
        );
    }

    activateScene(scene) {
        for (const entry of this.media.values()) {
            if (entry.type === "video") {
                entry.element.pause();
            }
        }

        const entry = this.media.get(`${scene.assetType}:${scene.asset}`);
        if (!entry || entry.type !== "video" || !entry.ready) {
            return;
        }

        const video = entry.element;
        const usableDuration = Number.isFinite(video.duration) ? video.duration : 0;
        const desiredStart = usableDuration * clamp(scene.assetStartRatio, 0, 1);
        const safeStart = usableDuration > 0
            ? Math.min(desiredStart, Math.max(0, usableDuration - 0.05))
            : 0;

        try {
            video.currentTime = safeStart;
        } catch {
        }

        video.playbackRate = scene.playbackRate;
        video.play().catch(() => {});
    }

    drawScene(scene, progress, absoluteTime) {
        const context = this.context;
        const width = this.canvas.width;
        const height = this.canvas.height;
        const entry = this.media.get(`${scene.assetType}:${scene.asset}`);

        context.save();
        context.fillStyle = "#03050a";
        context.fillRect(0, 0, width, height);

        const motion = resolveMotion(scene, progress, absoluteTime, width, height);
        const mediaFilter = resolveFilter(scene);

        context.translate(width / 2 + motion.x, height / 2 + motion.y);
        context.rotate((scene.rotation + motion.rotation) * Math.PI / 180);
        context.scale(motion.zoom, motion.zoom);
        context.translate(-width / 2, -height / 2);
        context.filter = mediaFilter;

        if (entry?.ready) {
            this.drawMedia(entry, width, height);
        } else {
            this.drawFallback(scene, width, height, progress);
        }

        context.restore();
        context.filter = "none";

        this.drawColorGrade(scene, width, height);
        this.drawLetterbox(width, height);
        this.drawGrain(scene, width, height, absoluteTime);
        this.drawTransition(scene, progress, width, height);

        if (scene.showMetadata) {
            this.drawMetadata(scene, width, height, progress);
        }

        if (scene.showTitle) {
            this.drawTitle(scene, width, height, progress);
        }
    }

    drawMedia(entry, width, height) {
        const element = entry.element;
        const sourceWidth = entry.type === "video" ? element.videoWidth : element.naturalWidth;
        const sourceHeight = entry.type === "video" ? element.videoHeight : element.naturalHeight;

        if (!sourceWidth || !sourceHeight) {
            return;
        }

        const sourceRatio = sourceWidth / sourceHeight;
        const targetRatio = width / height;

        let sx = 0;
        let sy = 0;
        let sw = sourceWidth;
        let sh = sourceHeight;

        if (sourceRatio > targetRatio) {
            sw = sourceHeight * targetRatio;
            sx = (sourceWidth - sw) / 2;
        } else {
            sh = sourceWidth / targetRatio;
            sy = (sourceHeight - sh) / 2;
        }

        this.context.drawImage(element, sx, sy, sw, sh, 0, 0, width, height);
    }

    drawFallback(scene, width, height, progress) {
        const context = this.context;
        const hue = scene.overlayHue;
        const gradient = context.createLinearGradient(0, 0, width, height);
        gradient.addColorStop(0, `hsl(${hue} 65% 16%)`);
        gradient.addColorStop(0.5, `hsl(${(hue + 35) % 360} 70% 7%)`);
        gradient.addColorStop(1, "#020309");
        context.fillStyle = gradient;
        context.fillRect(0, 0, width, height);

        context.save();
        context.globalAlpha = 0.18;
        context.translate(width * (0.25 + progress * 0.5), height * 0.45);
        context.rotate(-0.25);
        const streak = context.createLinearGradient(-width, 0, width, 0);
        streak.addColorStop(0, "transparent");
        streak.addColorStop(0.5, `hsl(${hue} 90% 70%)`);
        streak.addColorStop(1, "transparent");
        context.fillStyle = streak;
        context.fillRect(-width, -height * 0.08, width * 2, height * 0.16);
        context.restore();
    }

    drawColorGrade(scene, width, height) {
        const context = this.context;
        context.save();
        context.globalAlpha = scene.overlayOpacity;
        context.globalCompositeOperation = "screen";
        const gradient = context.createRadialGradient(
            width * 0.5,
            height * 0.45,
            0,
            width * 0.5,
            height * 0.45,
            Math.max(width, height) * 0.8
        );
        gradient.addColorStop(0, `hsla(${scene.overlayHue}, 85%, 60%, 0.45)`);
        gradient.addColorStop(1, `hsla(${scene.overlayHue}, 80%, 5%, 0)`);
        context.fillStyle = gradient;
        context.fillRect(0, 0, width, height);
        context.restore();

        context.save();
        const vignette = context.createRadialGradient(
            width / 2,
            height / 2,
            Math.min(width, height) * 0.15,
            width / 2,
            height / 2,
            Math.max(width, height) * 0.72
        );
        vignette.addColorStop(0, "rgba(0,0,0,0)");
        vignette.addColorStop(1, `rgba(0,0,0,${scene.vignetteOpacity})`);
        context.fillStyle = vignette;
        context.fillRect(0, 0, width, height);
        context.restore();
    }

    drawLetterbox(width, height) {
        const bar = height * 0.075;
        this.context.fillStyle = "#000";
        this.context.fillRect(0, 0, width, bar);
        this.context.fillRect(0, height - bar, width, bar);
    }

    drawGrain(scene, width, height, time) {
        const context = this.context;
        const count = Math.max(24, Math.floor(width * height / 24000));
        let state = hashString(`${this.descriptor.signature}:${Math.floor(time * 24)}`);

        context.save();
        context.globalAlpha = scene.grainOpacity;

        for (let index = 0; index < count; index++) {
            state = xorshift(state);
            const x = (state >>> 0) / 4294967295 * width;
            state = xorshift(state);
            const y = (state >>> 0) / 4294967295 * height;
            state = xorshift(state);
            const size = 0.5 + ((state >>> 0) / 4294967295) * 1.6;
            context.fillStyle = index % 2 === 0 ? "#fff" : "#000";
            context.fillRect(x, y, size, size);
        }

        context.restore();
    }

    drawTransition(scene, progress, width, height) {
        const context = this.context;
        const ratio = clamp(scene.transitionDurationRatio, 0.01, 0.49);
        const enter = clamp(progress / ratio, 0, 1);
        const exit = clamp((1 - progress) / ratio, 0, 1);
        const edge = Math.min(enter, exit);
        const intensity = 1 - edge;

        if (intensity <= 0) {
            return;
        }

        context.save();

        switch (scene.transition) {
            case "flash":
                context.fillStyle = `rgba(255,255,255,${intensity * 0.72})`;
                context.fillRect(0, 0, width, height);
                break;
            case "wipe-left":
                context.fillStyle = "#000";
                context.fillRect(width * (1 - intensity), 0, width * intensity, height);
                break;
            case "wipe-right":
                context.fillStyle = "#000";
                context.fillRect(0, 0, width * intensity, height);
                break;
            case "glitch":
                for (let index = 0; index < 7; index++) {
                    const bandHeight = height / 14;
                    const y = ((index * 137) % 13) * bandHeight;
                    context.fillStyle = index % 2 === 0
                        ? `hsla(${scene.overlayHue},90%,60%,${intensity * 0.22})`
                        : `rgba(255,255,255,${intensity * 0.08})`;
                    context.fillRect((index % 3 - 1) * width * 0.04 * intensity, y, width, bandHeight);
                }
                break;
            case "blur":
            case "zoom":
            case "fade":
            default:
                context.fillStyle = `rgba(0,0,0,${intensity * 0.92})`;
                context.fillRect(0, 0, width, height);
                break;
        }

        context.restore();
    }

    drawMetadata(scene, width, height, progress) {
        const context = this.context;
        const alpha = smoothStep(clamp(progress * 3, 0, 1));

        context.save();
        context.globalAlpha = alpha * 0.88;
        context.fillStyle = "rgba(255,255,255,0.92)";
        context.font = `600 ${Math.max(15, width * 0.015)}px Inter, sans-serif`;
        context.textAlign = "left";
        context.fillText(`${this.descriptor.genre.toUpperCase()}  •  ${this.descriptor.year}`, width * 0.075, height * 0.84);
        context.restore();
    }

    drawTitle(scene, width, height, progress) {
        const context = this.context;
        const eased = easeOutCubic(clamp(progress * 2.2, 0, 1));
        const title = this.descriptor.title.toUpperCase();
        const baseSize = Math.max(34, Math.min(width * 0.068, height * 0.115));

        context.save();
        context.textAlign = "center";
        context.textBaseline = "middle";
        context.fillStyle = "#fff";
        context.shadowColor = `hsla(${scene.overlayHue},90%,65%,0.8)`;
        context.shadowBlur = baseSize * 0.3;
        context.globalAlpha = eased;

        let x = width / 2;
        let y = height / 2;
        let scale = 1;

        switch (scene.titleStyle) {
            case "impact":
                scale = 1.45 - eased * 0.45;
                break;
            case "rise":
                y += (1 - eased) * height * 0.13;
                break;
            case "slide":
                x += (1 - eased) * width * 0.25;
                break;
            case "whisper":
                context.globalAlpha = eased * 0.72;
                scale = 0.92 + eased * 0.08;
                break;
            case "neon":
                context.shadowBlur = baseSize * 0.7;
                break;
            case "tracking":
            default:
                scale = 0.86 + eased * 0.14;
                break;
        }

        context.translate(x, y);
        context.scale(scale, scale);
        context.font = `800 ${baseSize}px Inter, sans-serif`;
        fitText(context, title, width * 0.8, baseSize);
        context.fillText(title, 0, 0);

        context.font = `500 ${Math.max(14, baseSize * 0.28)}px Inter, sans-serif`;
        context.shadowBlur = 0;
        context.globalAlpha *= 0.75;
        context.fillText(`${this.descriptor.genre} • ${this.descriptor.year}`, 0, baseSize * 0.9);
        context.restore();
    }
}

function resolveMotion(scene, progress, time, width, height) {
    const baseZoom = lerp(scene.zoomFrom, scene.zoomTo, easeInOut(progress));
    let x = scene.panX * width * progress;
    let y = scene.panY * height * progress;
    let zoom = baseZoom;
    let rotation = 0;

    switch (scene.motion) {
        case "push-in":
            zoom *= 1 + progress * 0.045;
            break;
        case "pull-out":
            zoom *= 1.045 - progress * 0.045;
            break;
        case "pan-left":
            x -= width * 0.05 * progress;
            break;
        case "pan-right":
            x += width * 0.05 * progress;
            break;
        case "pan-up":
            y -= height * 0.05 * progress;
            break;
        case "pan-down":
            y += height * 0.05 * progress;
            break;
        case "orbit":
            x += Math.cos(progress * Math.PI * 2) * width * 0.015;
            y += Math.sin(progress * Math.PI * 2) * height * 0.015;
            rotation += Math.sin(progress * Math.PI * 2) * 0.35;
            break;
        case "handheld": {
            const shake = scene.shake;
            x += Math.sin(time * 43.1 + scene.index * 2.7) * shake * width;
            y += Math.cos(time * 37.7 + scene.index * 3.9) * shake * height;
            rotation += Math.sin(time * 29.3) * shake * 10;
            break;
        }
    }

    return { x, y, zoom, rotation };
}

function resolveFilter(scene) {
    const base = `brightness(${scene.brightness}) contrast(${scene.contrast}) saturate(${scene.saturation})`;

    switch (scene.filter) {
        case "cold":
            return `${base} hue-rotate(190deg) saturate(${scene.saturation * 0.8})`;
        case "warm":
            return `${base} sepia(0.18) saturate(${scene.saturation * 1.12})`;
        case "noir":
            return `${base} grayscale(0.82) contrast(${scene.contrast * 1.12})`;
        case "neon":
            return `${base} saturate(${scene.saturation * 1.45}) contrast(${scene.contrast * 1.08})`;
        case "desaturated":
            return `${base} saturate(${scene.saturation * 0.58})`;
        case "high-contrast":
            return `${base} contrast(${scene.contrast * 1.2})`;
        default:
            return base;
    }
}

function waitForVideo(video) {
    if (video.readyState >= HTMLMediaElement.HAVE_METADATA) {
        return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
        const onReady = () => {
            cleanup();
            resolve();
        };
        const onError = () => {
            cleanup();
            reject(new Error(`Unable to load video: ${video.src}`));
        };
        const cleanup = () => {
            video.removeEventListener("loadedmetadata", onReady);
            video.removeEventListener("error", onError);
        };

        video.addEventListener("loadedmetadata", onReady, { once: true });
        video.addEventListener("error", onError, { once: true });
        video.load();
    });
}

function waitForAudio(audio) {
    if (audio.readyState >= HTMLMediaElement.HAVE_METADATA) {
        return Promise.resolve();
    }

    return new Promise(resolve => {
        const done = () => {
            audio.removeEventListener("loadedmetadata", done);
            audio.removeEventListener("error", done);
            resolve();
        };

        audio.addEventListener("loadedmetadata", done, { once: true });
        audio.addEventListener("error", done, { once: true });
        audio.load();
    });
}

function fitText(context, text, maximumWidth, initialSize) {
    let size = initialSize;
    while (size > 18 && context.measureText(text).width > maximumWidth) {
        size -= 2;
        context.font = `800 ${size}px Inter, sans-serif`;
    }
}

function hashString(value) {
    let hash = 2166136261;
    for (let index = 0; index < value.length; index++) {
        hash ^= value.charCodeAt(index);
        hash = Math.imul(hash, 16777619);
    }
    return hash >>> 0;
}

function xorshift(value) {
    let state = value | 0;
    state ^= state << 13;
    state ^= state >>> 17;
    state ^= state << 5;
    return state >>> 0;
}

function smoothStep(value) {
    return value * value * (3 - 2 * value);
}

function easeOutCubic(value) {
    return 1 - Math.pow(1 - value, 3);
}

function easeInOut(value) {
    return value < 0.5
        ? 2 * value * value
        : 1 - Math.pow(-2 * value + 2, 2) / 2;
}

function lerp(start, end, amount) {
    return start + (end - start) * amount;
}

function clamp(value, minimum, maximum) {
    return Math.min(maximum, Math.max(minimum, value));
}
