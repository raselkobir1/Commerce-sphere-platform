(function () {
  var root = document.documentElement;
  var stored = localStorage.getItem("csg-theme");
  if (stored) root.setAttribute("data-theme", stored);

  function currentTheme() {
    var attr = root.getAttribute("data-theme");
    if (attr) return attr;
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }

  function setToggleLabel(btn) {
    btn.textContent = currentTheme() === "dark" ? "☀️ Light" : "🌙 Dark";
  }

  document.addEventListener("DOMContentLoaded", function () {
    var btn = document.getElementById("theme-toggle");
    if (btn) {
      setToggleLabel(btn);
      btn.addEventListener("click", function () {
        var next = currentTheme() === "dark" ? "light" : "dark";
        root.setAttribute("data-theme", next);
        localStorage.setItem("csg-theme", next);
        setToggleLabel(btn);
      });
    }

    if (window.hljs) {
      document.querySelectorAll("pre code").forEach(function (el) {
        hljs.highlightElement(el);
      });
    }

    if (window.mermaid) {
      mermaid.initialize({
        startOnLoad: true,
        theme: "dark",
        themeVariables: {
          background: "#0b0d13",
          primaryColor: "#1c2030",
          primaryTextColor: "#e7e9f2",
          primaryBorderColor: "#3a3f55",
          lineColor: "#5b6072",
          secondaryColor: "#171a24",
          tertiaryColor: "#0b0d13",
          fontFamily: "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace",
        },
        securityLevel: "loose",
      });
    }

    var path = window.location.pathname.split("/").pop() || "index.html";
    document.querySelectorAll(".topnav a").forEach(function (a) {
      var href = a.getAttribute("href").split("/").pop();
      if (href === path) a.classList.add("active");
    });
  });
})();
