<template>
  <h1>Sessions</h1>

  <div>
    <h2>Active Session</h2>
    <div v-if="activeSession != null">
      <p>Active Session Data!</p>
      <p>
        Name: <span>{{ activeSession.name }}</span>
      </p>
      <a v-on:click="endSession">End Session</a>
    </div>
    <div v-else>
      <p>There is no currently active session.</p>
      <a v-on:click="beginSession">Begin Session</a>
    </div>
  </div>
  <div id="session-history">
    <h2>History</h2>
    <div class="items" :data-is-loading="isHistoryLoading">
      <p class="placeholder">There are no items...</p>
    </div>
  </div>
</template>

<script lang="ts">
interface ITimeManSession {
  name: string;
  start: string;
}

interface ISessionsModel {
  isHistoryLoading: false;
  activeSession: ITimeManSession;
}

export default {
  name: "Sessions",
  data(): ISessionsModel {
    return {
      isHistoryLoading: false,
      activeSession: null,
      // history: {
      //   // Pagination.
      //   // List of Sessions.
      //   sessions: []
      // }
    };
  },
  methods: {
    beginSession: function () {
      console.log("starting a new session!");

      // Indicate loading / API status.
      // Ping the server to start the session.
      // Get the data:
      // Display it!
      // End the load stuff....

      let sess: ITimeManSession = {
        name: "my Session",
        start: "now",
      };
      this.activeSession = sess;
    },
    endSession: function () {
      console.log("ending the session!");
      this.activeSession = null;
    },
  },
  mounted: function () {
    // Indicate loading / GET status.
    // Grab the currently active session.
    // diplay / end loading.
    // Then get history (last n items).
    // display data + end loading on history part.

    // let x = this.$refs;
    this.isHistoryLoading = false;
  },
};
</script>
