// A medal hanging from a pinned thread, simulated with Verlet integration.
//
// The thread is a chain of light particles joined by fixed-length links. The medal is two particles —
// the point the thread ties onto (the top edge of the sprite) and its heavy centre — joined by a rigid
// link, so it swings and spins with real angular momentum instead of just pointing along the last
// thread segment.
//
// Everything is simulated in the overlay's own coordinates, and the overlay is part of the page, so
// the browser scrolls it with everything else: scrolling never moves the medal relative to the page.
// (An earlier version simulated in viewport coordinates on a fixed overlay, and every scroll step
// teleported the pin and yanked the thread — and lagged behind the browser's off-thread scrolling.)
//
// Both parts are sprites. The medal sprite is scaled to fit; the thread sprite is tiled along the
// thread, one rotated strip per segment, so it bends and stretches with it.

const GRAVITY = 2400;          // px/s²
const STEP = 1 / 120;          // fixed physics step
const MAX_FRAME = 1 / 20;      // don't try to catch up after the tab was hidden
const ITERATIONS = 16;         // constraint relaxation passes per step
const DAMPING = 0.996;         // per-step velocity retention (air drag)
const MAX_SPEED = 60;          // px per step; keeps a violent fling from exploding the chain

const SEGMENTS = 9;
const SEGMENT_LENGTH = 15;
const THREAD_MASS = 1;
const BAIL_MASS = 1;
const MEDAL_MASS = 8;

const FACE_MAX_WIDTH = 80;     // the medal sprite is scaled to fit this box, keeping its proportions
const FACE_MAX_HEIGHT = 110;
const THREAD_WIDTH = 8;        // the thread sprite is drawn as a strip this wide

export async function attach(svg) {
    const medal = svg.querySelector('[data-part="medal"]');
    const pin = svg.querySelector('[data-part="pin"]');

    // The thread ties onto the top edge of the medal sprite.
    const faceImage = svg.querySelector('[data-part="face-image"]');
    const face = await naturalSize(faceImage);
    const scale = Math.min(FACE_MAX_WIDTH / face.width, FACE_MAX_HEIGHT / face.height);
    const faceWidth = face.width * scale, faceHeight = face.height * scale;
    setAttributes(faceImage, { x: -faceWidth / 2, y: -faceHeight / 2, width: faceWidth, height: faceHeight });
    const bailToCentre = faceHeight / 2;

    // The texture runs along the thread, so its long side follows the thread: a landscape image is
    // turned on its side to make the strip.
    const threadImage = svg.querySelector('[data-part="thread-image"]');
    const texture = await naturalSize(threadImage);
    const landscape = texture.width > texture.height;
    const tile = Math.max(1, THREAD_WIDTH * (landscape ? texture.width / texture.height : texture.height / texture.width));
    setAttributes(threadImage.parentElement, { x: -THREAD_WIDTH / 2, y: 0, width: THREAD_WIDTH, height: tile });
    if (landscape)
        setAttributes(threadImage, { width: tile, height: THREAD_WIDTH, transform: `translate(${THREAD_WIDTH} 0) rotate(90)` });
    else
        setAttributes(threadImage, { width: THREAD_WIDTH, height: tile });

    // Each strip sits in the thread's own length coordinates (segment i covers i·L to (i+1)·L), so the
    // pattern carries on seamlessly from one segment to the next. They overlap slightly to hide seams
    // at the bends.
    const strips = [];
    const group = svg.querySelector('[data-part="thread-segments"]');
    for (let i = 0; i < SEGMENTS; i++) {
        const strip = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        setAttributes(strip, {
            x: -THREAD_WIDTH / 2, y: i * SEGMENT_LENGTH - 0.75,
            width: THREAD_WIDTH, height: SEGMENT_LENGTH + 1.5,
            fill: `url(#${threadImage.parentElement.id})`,
        });
        group.appendChild(strip);
        strips.push(strip);
    }

    const reach = SEGMENTS * SEGMENT_LENGTH + bailToCentre;

    // Viewport (client) coordinates to the overlay's coordinates. Read fresh each time, so it holds
    // however far the page has scrolled.
    function toLocal(clientX, clientY) {
        const origin = svg.getBoundingClientRect();
        return { x: clientX - origin.left, y: clientY - origin.top };
    }

    // Hangs off the bottom edge of the navbar near the right side.
    const navBottom = document.querySelector('.navbar')?.getBoundingClientRect().bottom ?? 64;
    const root = toLocal(Math.max(60, window.innerWidth - 90), navBottom + 6);

    const particle = (x, y, mass) => ({ x, y, px: x, py: y, w: 1 / mass });
    const points = [];
    for (let i = 0; i <= SEGMENTS; i++)
        points.push(particle(root.x, root.y + i * SEGMENT_LENGTH, i === SEGMENTS ? BAIL_MASS : THREAD_MASS));
    const bail = points[SEGMENTS];
    const centre = particle(bail.x, bail.y + bailToCentre, MEDAL_MASS);
    points.push(centre);
    const head = points[0];
    head.w = 0;

    const links = [];
    for (let i = 0; i < SEGMENTS; i++) links.push([points[i], points[i + 1], SEGMENT_LENGTH]);
    links.push([bail, centre, bailToCentre]);

    // Start with a nudge so the medal swings into place instead of appearing perfectly still.
    centre.px -= 3;

    let grab = null;           // { part: 'root' | 'medal', pointerId, dx, dy }
    let pointer = { x: 0, y: 0 }; // client coordinates, converted each frame in case the page scrolled
    const medalWeight = centre.w;

    function clampToViewport(p, margin) {
        return {
            x: Math.min(Math.max(p.x, margin), window.innerWidth - margin),
            y: Math.min(Math.max(p.y, margin), window.innerHeight - margin),
        };
    }

    // Moves the whole medal without giving it any velocity, for when the pin has to jump.
    function shift(dx, dy) {
        for (const p of points) {
            p.x += dx; p.y += dy;
            p.px += dx; p.py += dy;
        }
    }

    // Where the medal centre should be while held: under the pointer, but never further from the
    // root than the thread can reach.
    function medalTarget(anchor) {
        const held = toLocal(pointer.x, pointer.y);
        let x = held.x + grab.dx, y = held.y + grab.dy;
        const dx = x - anchor.x, dy = y - anchor.y;
        const d = Math.hypot(dx, dy);
        if (d > reach) {
            x = anchor.x + dx / d * reach;
            y = anchor.y + dy / d * reach;
        }
        return { x, y };
    }

    function satisfy(a, b, length) {
        const dx = b.x - a.x, dy = b.y - a.y;
        const d = Math.hypot(dx, dy) || 1e-6;
        const total = a.w + b.w;
        if (total === 0) return;
        const k = (d - length) / d / total;
        a.x += dx * k * a.w; a.y += dy * k * a.w;
        b.x -= dx * k * b.w; b.y -= dy * k * b.w;
    }

    function step(headTo, medalTo) {
        for (const p of points) {
            if (p.w === 0) continue;
            let vx = (p.x - p.px) * DAMPING, vy = (p.y - p.py) * DAMPING;
            const speed = Math.hypot(vx, vy);
            if (speed > MAX_SPEED) { vx *= MAX_SPEED / speed; vy *= MAX_SPEED / speed; }
            p.px = p.x; p.py = p.y;
            p.x += vx;
            p.y += vy + GRAVITY * STEP * STEP;
        }

        // Pinned particles are moved, not integrated, but their previous position is kept so they
        // carry the drag velocity the moment they're let go.
        head.px = head.x; head.py = head.y;
        head.x = headTo.x; head.y = headTo.y;
        if (medalTo) {
            centre.px = centre.x; centre.py = centre.y;
            centre.x = medalTo.x; centre.y = medalTo.y;
        }

        for (let i = 0; i < ITERATIONS; i++) {
            for (const [a, b, length] of links) satisfy(a, b, length);
            // The chain only converges approximately; hard-cap the total reach so a fast drag can
            // never stretch the thread visibly.
            const dx = centre.x - head.x, dy = centre.y - head.y;
            if (Math.hypot(dx, dy) > reach) satisfy(head, centre, reach);
        }
    }

    let last = performance.now();
    let carry = 0;
    let frame = 0;

    function tick(now) {
        const elapsed = Math.min(Math.max(now - last, 0) / 1000, MAX_FRAME);
        last = now;
        carry += elapsed;

        if (grab?.part === 'root') {
            const p = clampToViewport(pointer, 4);
            Object.assign(root, toLocal(p.x, p.y));
        }

        // Spread this frame's movement of the held points across the sub-steps so the velocity they
        // hand over on release is the real drag speed, not one frame's jump squeezed into one step.
        const steps = Math.floor(carry / STEP);
        carry -= steps * STEP;
        const headFrom = { x: head.x, y: head.y };
        const headTo = { x: root.x, y: root.y };
        const medalFrom = { x: centre.x, y: centre.y };
        const medalTo = grab?.part === 'medal' ? medalTarget(headTo) : null;
        for (let i = 1; i <= steps; i++) {
            const t = i / steps;
            step(
                { x: headFrom.x + (headTo.x - headFrom.x) * t, y: headFrom.y + (headTo.y - headFrom.y) * t },
                medalTo && { x: medalFrom.x + (medalTo.x - medalFrom.x) * t, y: medalFrom.y + (medalTo.y - medalFrom.y) * t },
            );
        }

        draw();
        frame = requestAnimationFrame(tick);
    }

    function draw() {
        for (let i = 0; i < strips.length; i++) {
            const a = points[i], b = points[i + 1];
            const dx = b.x - a.x, dy = b.y - a.y;
            const angle = Math.atan2(dy, dx) * 180 / Math.PI - 90;
            const stretch = Math.hypot(dx, dy) / SEGMENT_LENGTH;
            strips[i].setAttribute('transform',
                `translate(${a.x.toFixed(1)} ${a.y.toFixed(1)}) rotate(${angle.toFixed(2)}) scale(1 ${stretch.toFixed(3)}) translate(0 ${-i * SEGMENT_LENGTH})`);
        }

        const angle = Math.atan2(bail.x - centre.x, -(bail.y - centre.y)) * 180 / Math.PI;
        medal.setAttribute('transform', `translate(${centre.x.toFixed(1)} ${centre.y.toFixed(1)}) rotate(${angle.toFixed(2)})`);
        pin.setAttribute('transform', `translate(${head.x.toFixed(1)} ${head.y.toFixed(1)})`);
    }

    function onPointerDown(part) {
        return e => {
            if (grab || (e.pointerType === 'mouse' && e.button !== 0)) return;
            e.preventDefault();
            pointer = { x: e.clientX, y: e.clientY };
            grab = { part, pointerId: e.pointerId, dx: 0, dy: 0 };
            if (part === 'medal') {
                const held = toLocal(e.clientX, e.clientY);
                grab.dx = centre.x - held.x;
                grab.dy = centre.y - held.y;
                centre.w = 0;
            }
            // Set on the root so the cursor stays put when a fast drag outruns the medal.
            document.documentElement.style.cursor = 'grabbing';
        };
    }

    function onPointerMove(e) {
        if (grab && e.pointerId === grab.pointerId) pointer = { x: e.clientX, y: e.clientY };
    }

    function onPointerUp(e) {
        if (!grab || e.pointerId !== grab.pointerId) return;
        if (grab.part === 'medal') centre.w = medalWeight;
        grab = null;
        document.documentElement.style.cursor = '';
    }

    function onResize() {
        // Keep the pin reachable if the window shrinks out from under it. The medal moves with it as
        // one piece; letting the physics chase a pin that jumped would fling it across the screen.
        if (grab) return;
        const limit = toLocal(window.innerWidth - 20, 0).x;
        if (root.x > limit) {
            const dx = limit - root.x;
            root.x += dx;
            shift(dx, 0);
        }
    }

    const grabRoot = onPointerDown('root');
    const grabMedal = onPointerDown('medal');
    pin.addEventListener('pointerdown', grabRoot);
    medal.addEventListener('pointerdown', grabMedal);
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('pointercancel', onPointerUp);
    window.addEventListener('resize', onResize);

    draw();
    svg.style.visibility = 'visible';
    frame = requestAnimationFrame(tick);

    return {
        dispose() {
            cancelAnimationFrame(frame);
            if (grab) document.documentElement.style.cursor = '';
            pin.removeEventListener('pointerdown', grabRoot);
            medal.removeEventListener('pointerdown', grabMedal);
            window.removeEventListener('pointermove', onPointerMove);
            window.removeEventListener('pointerup', onPointerUp);
            window.removeEventListener('pointercancel', onPointerUp);
            window.removeEventListener('resize', onResize);
        },
    };
}

function setAttributes(element, attributes) {
    for (const [name, value] of Object.entries(attributes))
        element.setAttribute(name, typeof value === 'number' ? value.toFixed(2) : value);
}

// The pixel size of an <image>'s source, so it can be drawn in proportion. A broken or stalled image
// falls back to a square rather than stopping the medal from appearing at all. Uses the load event
// rather than decode(), which waits for the page to be painted and so can hang in a background tab.
function naturalSize(image) {
    return new Promise(resolve => {
        const img = new Image();
        const done = () => resolve(img.naturalWidth > 0 && img.naturalHeight > 0
            ? { width: img.naturalWidth, height: img.naturalHeight }
            : { width: 1, height: 1 });
        img.onload = img.onerror = done;
        setTimeout(done, 5000);
        img.src = image.href.baseVal;
    });
}
