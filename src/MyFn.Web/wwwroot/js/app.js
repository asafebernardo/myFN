window.charts = {
    instances: {},
    render: function (id, config) {
        const el = document.getElementById(id);
        if (!el || typeof Chart === "undefined") {
            return;
        }
        if (this.instances[id]) {
            this.instances[id].destroy();
        }
        this.instances[id] = new Chart(el, config);
    }
};

window.downloadFile = function (name, type, base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = name;
    a.click();
    URL.revokeObjectURL(url);
};
