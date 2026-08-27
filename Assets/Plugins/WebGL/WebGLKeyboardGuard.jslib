mergeInto(LibraryManager.library, {
  InstallWebGLKeyboardGuard: function () {
    if (window.__neonCircuitKeyboardGuardInstalled) {
      return;
    }

    var canvas = document.getElementById("unity-canvas") || document.querySelector("canvas");
    if (!canvas) {
      return;
    }

    window.__neonCircuitKeyboardGuardInstalled = true;
    canvas.tabIndex = 0;

    canvas.addEventListener("pointerdown", function () {
      canvas.focus({ preventScroll: true });
    });

    window.addEventListener("keydown", function (event) {
      if (document.activeElement !== canvas && document.pointerLockElement !== canvas) {
        return;
      }

      if (
        event.code === "ArrowUp" ||
        event.code === "ArrowDown" ||
        event.code === "ArrowLeft" ||
        event.code === "ArrowRight" ||
        event.code === "Space"
      ) {
        event.preventDefault();
      }
    }, { capture: true, passive: false });
  }
});
