function startTimer(countDownDate, ID) {

    var clientTimeZone = new Date().getTimezoneOffset();

    var now = new Date().getTime() + (clientTimeZone) * 60 * 1000;

    var distance = countDownDate - now;

    var days = Math.floor(distance / (1000 * 60 * 60 * 24));
    var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
    var seconds = Math.floor((distance % (1000 * 60)) / 1000);
    
    if (distance > 0) {
        document.getElementById(ID).innerHTML = days + "d " + hours + "h " + minutes + "m " + seconds + "s ";
    }
    
    if (distance <= 0) {
        document.getElementById(ID).innerHTML = "EXPIRED";
        document.getElementById("button " + ID).disabled = true;
    }
}