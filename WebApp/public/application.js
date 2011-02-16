function iReturnStaticString() {
  return 'static string!';
}

function iReturnStringUsingParameterPassed(param) {
  return 'you passed: ' + param;
}

function iReturnStringUsingParametersPassed() {
  var args = [];
  for (var i = 0; i < arguments.length; i++) args[args.length] = arguments[i];
  return 'you passed: ' + args.join(', ');
}

$(document).ready(function(){
    
});
