// This example shows how to render pages that perform AJAX calls
// upon page load.
//
// Instead of waiting a fixed amount of time before doing the render,
// we are keeping track of every resource that is loaded.
//
// Once all resources are loaded, we wait a small amount of time
// (resourceWait) in case these resources load other resources.
//
// The page is rendered after a maximum amount of time (maxRenderTime)
// or if no new resources are loaded.
 
var resourceWait  = 300,
    maxRenderWait = 10000;
 
var page          = require('webpage').create(),
    system = require('system'),
    count         = 0,
    forcedRenderTimeout,
    renderTimeout;
 
page.viewportSize = { width: 1280, height : 1024 };
 
function doRender() {
    console.log(page.content);
    phantom.exit();
}
 
page.onResourceRequested = function (req) {
    count += 1;
    //console.log('> ' + req.id + ' - ' + req.url);
    clearTimeout(renderTimeout);
};
 
page.onResourceReceived = function (res) {
    if (!res.stage || res.stage === 'end') {
        count -= 1;
        //console.log(res.id + ' ' + res.status + ' - ' + res.url);
        if (count === 0) {
            renderTimeout = setTimeout(doRender, resourceWait);
        }
    }
};
 
page.open(system.args[1], function (status) {
    if (status !== "success") {
        console.log('Error : Unable to load url');
        phantom.exit();
    } else {
        forcedRenderTimeout = setTimeout(function () {
            console.log(count);
            doRender();
        }, maxRenderWait);
    }
});