// This store is what we use to manage login + permission states.
// It integrates with a MemberMan backend, and so the name of the file/store may change in the future to be more specific.


import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useLoginStore = defineStore('login', () => {


  function GetState() {
    alert('wow!');
  }

});