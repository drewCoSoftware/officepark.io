<template>
  <h1>Sessions</h1>

  <div>
    <h2>Active Session</h2>
    <div v-if="activeSession != null">
      <p>Active Session Data!</p>
      <a v-on:click="endSession">End Session</a>
    </div>
    <div v-else>
      <p>There is no currently active session.</p>
      <a v-on:click="beginSession">Begin Session</a>
    </div>
  </div>
  <div>
    <h2>History</h2>
  </div>
</template>

<script>
export default {
  name: "Sessions",
  data() {
    return {
      activeSession: null,
      history: {
        // Pagination.
        // List of Sessions.
        sessions: []
      }
    };
  },
  methods: {
    'beginSession': function(){
      console.log('starting a new session!');
      this.activeSession = {
        start: 'now'
      };
    },
    'endSession': function() {
      console.log('ending the session!');
      this.activeSession = null;
    }
  }
};
</script>
