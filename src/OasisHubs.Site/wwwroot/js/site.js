(async () => {

   let form = document.querySelector('.form-with-spinner');
   if (form) {
      form.addEventListener('submit', function (e) {
         document.querySelector("#spinner").classList.remove('hidden');
      });
   }
})();
