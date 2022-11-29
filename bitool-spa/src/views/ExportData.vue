<template>
  <div>
    <section class="section is-main-section">
      <div class="columns">
        <div class="column is-11  m-0 pb-0">
          <b-field grouped class="mb-3">
          <div class="mr-3">
            <p class="title is-6">Campaigns</p>
            <p class="subtitle is-6">Total Times Exported from</p>
          </div>
          <b-field>
            <b-input v-model="filter.totalTimesExportedFrom" type="number"></b-input>
          </b-field>
          <div class="mr-3">
            <p class="title is-6 has-text-white-bis">.</p>
            <p class="subtitle is-6">to</p>
          </div>
          <b-field>
            <b-input v-model="filter.totalTimesExportedTo" type="number"></b-input>
          </b-field>       
        </b-field>
        </div>
        <div class="column  m-0 pb-0">
          <b-button           
            class="button"
            @click="isImageModalActive = true" 
            style="padding: 0; border: none; background: none;">
            <b-icon
              icon="file-image-outline"
              size="is-medium"
              type="is-info">
            </b-icon>
          </b-button>
        </div>
      </div>

      <b-field grouped class="mb-3">
        <div class="mr-3">
          <p :class="isValidFilter || hasDateLastExport?
            'subtitle is-6 pt-3'
            :'subtitle is-6 pt-3 has-text-danger'">Date Last Export
            <span :class="hasDateLastExport?'has-text-white': 'has-text-danger'">* </span> 
            <span class="has-text-black">from</span>
            </p>
        </div>
        <b-field>
          <b-datepicker
            icon="calendar-today"
            locale="en-CA"
            v-model="dateLastExportedFrom"
            editable
            required
            aria-required="dateLastExportedFrom"
            ref="dateLastExportedFrom"
          >
          </b-datepicker>
        </b-field>
        <div class="mr-3">
          <p class="subtitle is-6 pt-3">to</p>
        </div>
        <b-field>
          <b-datepicker
            icon="calendar-today"
            locale="en-CA"
            v-model="dateLastExportedTo"
            editable
            required
            aria-required="dateLastExportedTo"
            ref="dateLastExportedTo"
          >
          </b-datepicker>
        </b-field> 
      </b-field>

      <b-field class="mb-3">
        <div class="mr-3">
          <p :class="isValidFilter || hasTaggedCampagin?
            'subtitle is-6 pt-3'
            : 'subtitle is-6 pt-3 has-text-danger'">
            Tagged Campaign
            <span :class="hasTaggedCampagin?'has-text-white': 'has-text-danger'">* </span> 
          </p>
        </div>
        <b-field>
          <multiselect
            v-model="selectAssignedCampaign"
            tag-placeholder=""
            placeholder="Select Tagged campaign"
            label="name"
            track-by="id"
            :options="customizedAdminCampaigns"
            selectLabel="Add"
            deselectLabel="Remove"
            ref="multiselectSelectAssignedCampaign"
          ></multiselect>
        </b-field>
         
        <div class="ml-3" v-show="customersOfTaggedCampagignCount!== null">
          <p class="subtitle is-6 pt-3">
            {{customersOfTaggedCampagignCount|formattedNumber}}
            customer mobile numbers
          </p>
        </div>        
      </b-field>

      <b-field grouped class="mb-3">
        <div class="mr-3">
          <p class="subtitle is-6 pt-3">Last 3 Campaigns Used</p>
        </div>
        <b-field>
          <multiselect
            v-model="selectLast3CampaignsUsed"
            tag-placeholder=""
            placeholder="Select campaigns"
            label="name"
            track-by="id"
            :options="adminCampaigns"
            :multiple="true"
            :max="3"
            :taggable="true"
            selectLabel="Add"
            deselectLabel="Remove"
          >
          <template slot="maxElements" >
            <p class="subtitle is-6 has-text-danger my-2">Maximum of 3 options selected.</p>
            <p class="subtitle is-6 has-text-danger my-2">First remove a selected option to select another.</p>
          </template>
          </multiselect>
        </b-field>
      </b-field>


      <b-field grouped class="mb-3">
        <div class="mr-3">
          <p class="title is-6">Others</p>
          <p :class="isValidFilter || hasTotalNumber?
            'subtitle is-6'
            :'subtitle is-6 has-text-danger'">Total Number to export
            <span :class="hasTotalNumber?'has-text-white': 'has-text-danger'">* </span> 
            </p>
        </div>
        <b-field>
          <b-input v-model="filter.exportTop" type="number"></b-input> 
        </b-field>        
      </b-field>

      <h5 class="subtitle is-6 mb-1">
        <span  v-show="isShowResult">Found {{ formattedTotalCount }} customer mobile numbers</span>
        <span v-show="!isShowResult" class="has-text-white-bis">.</span>
      </h5>

      <b-field grouped>
        <b-button :icon-left="isShowDateFirstAddedFrom? 'chevron-up':'chevron-down'" @click="isShowDateFirstAddedFrom=!isShowDateFirstAddedFrom"/> 
        <div class="mx-3">
          <a @click="isShowDateFirstAddedFrom=!isShowDateFirstAddedFrom"><p class="title is-6"> Customer</p></a> 
          <p class="subtitle is-6" v-show="isShowDateFirstAddedFrom">Date First Added from</p>
        </div>
        <b-field v-show="isShowDateFirstAddedFrom">
          <b-datepicker
            icon="calendar-today"
            locale="en-CA"
            v-model="dateFirstAddedFrom"
            editable                
          >
          </b-datepicker>
        </b-field>
        <div class="mr-3" v-show="isShowDateFirstAddedFrom">
          <p class="title is-6 has-text-white-bis">.</p>
          <p class="subtitle is-6">to</p>
        </div>
        <b-field v-show="isShowDateFirstAddedFrom">
          <b-datepicker
            icon="calendar-today"
            locale="en-CA"
            v-model="dateFirstAddedTo"
            editable
          >
          </b-datepicker>
        </b-field>
        <div v-show="isShowDateFirstAddedFrom">
          <p>
            <b-radio v-model="filter.sortingValue" size="is-small" native-value="DateFirstAdded asc">
              ASC 
            </b-radio>
          </p>
          <p>
            <b-radio v-model="filter.sortingValue" size="is-small" native-value="DateFirstAdded desc">
              DESC
            </b-radio>
          </p>
        </div>        
      </b-field>

      <b-field grouped class="mb-3">
        <b-button :icon-left="isShowOccurrence? 'chevron-up':'chevron-down'" @click="isShowOccurrence=!isShowOccurrence"/> 
        <div class="mx-3">
          <a @click="isShowOccurrence=!isShowOccurrence"><p class="title is-6">Occurrence (Indicators)</p></a> 
          <p class="subtitle is-6" v-show="isShowOccurrence">Date Last Occurred from</p>
        </div>
        <b-field v-show="isShowOccurrence">
          <b-datepicker
            icon="calendar-today"
            locale="en-CA"
            v-model="dateLastOccurredFrom"
            editable
          >
          </b-datepicker>
        </b-field>
        <div class="mr-3" v-show="isShowOccurrence">
          <p class="title is-6 has-text-white-bis">.</p>
          <p class="subtitle is-6">to</p>
        </div>
        <b-field v-show="isShowOccurrence">
          <b-datepicker
            icon="calendar-today"
            locale="en-CA"
            v-model="dateLastOccurredTo"
            editable
          >
          </b-datepicker>
        </b-field>         
      </b-field>

      <div style="margin-left: 52px;"  v-show="isShowOccurrence">
        <b-field grouped class="mb-3">
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">Occurred Categories</p>
          </div>
        <b-field>
          <multiselect
            v-model="selectOccurredCategories"
            tag-placeholder=""
            placeholder="Select Occurred Categories"
            label="scoreTitle"
            track-by="scoreID"
            :options="occurredCategories"
            :multiple="true"
            :taggable="true"
            selectLabel="Add"
            deselectLabel="Remove"
          >
          </multiselect>
          </b-field>
        </b-field>

        <b-field grouped class="mb-3">
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">Total Occurrence Points from</p>
          </div>
          <b-field>
            <b-input v-model="filter.totalOccurancePointsFrom" type="number"></b-input>
          </b-field>
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">to</p>
          </div>
          <b-field>
            <b-input v-model="filter.totalOccurancePointsTo" type="number"></b-input>
          </b-field>            
        </b-field>
      </div>
     
      
      <b-field grouped class="mb-3">
        <b-button :icon-left="isShowResults? 'chevron-up':'chevron-down'" @click="isShowResults=!isShowResults"/> 
        <div class="mx-3">
          <a @click="isShowResults=!isShowResults"><p class="title is-6">Results</p></a> 
          <p class="subtitle is-6" v-show="isShowResults">Results Categories</p>
        </div>
        <b-field v-show="isShowResults">
          <multiselect
            v-model="selectResultsCategories"
            tag-placeholder=""
            placeholder="Select Results Categories"
            label="scoreTitle"
            track-by="scoreID"
            :options="resultsCategories"
            :multiple="true"
            :taggable="true"
            selectLabel="Add"
            deselectLabel="Remove"
          >
          </multiselect>
        </b-field>
      </b-field>

      <b-field grouped class="mb-3" v-show="isShowResults" style="margin-left: 52px;">
        <div class="mr-3">
          <p class="subtitle is-6 mt-3">Total Results Points from</p>
        </div>
        <b-field>
          <b-input v-model="filter.totalResultsPointsFrom" type="number"></b-input>
        </b-field>
        <div class="mr-3">
          <p class="subtitle is-6 mt-3">to</p>
        </div>
        <b-field>
          <b-input v-model="filter.totalResultsPointsTo" type="number"></b-input>
        </b-field>            
      </b-field>

      <b-field grouped class="mb-3">
        <b-button :icon-left="isShowAnalysis? 'chevron-up':'chevron-down'" @click="isShowAnalysis=!isShowAnalysis"/> 
        <div class="mx-3">
          <a @click="isShowAnalysis=!isShowAnalysis"><p class="title is-6">Analysis</p></a> 
          <p class="subtitle is-6" v-show="isShowAnalysis">Total Points from</p>
        </div>
        <b-field v-show="isShowAnalysis">
          <b-input v-model="filter.totalPointsFrom" type="number"></b-input>
        </b-field>
         <div class="mr-3" v-show="isShowAnalysis">
          <p class="subtitle is-6 mt-3">to</p>
        </div>
        <b-field v-show="isShowAnalysis">
          <b-input v-model="filter.totalPointsTo" type="number"></b-input>
        </b-field>
      </b-field>

      <div style="margin-left: 52px;" v-show="isShowAnalysis">
        <b-field grouped class="mb-3">
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">Export vs Points (%) from</p>
          </div>
          <b-field>
            <b-input v-model="filter.exportVsPointsPercentageFrom" type="number"></b-input>
          </b-field>
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">to</p>
          </div>
          <b-field>
            <b-input v-model="filter.exportVsPointsPercentageTo" type="number"></b-input>
          </b-field>        
        </b-field>

        <b-field grouped class="mb-3">
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">Export vs Points Exceptions</p>
          </div>
          <b-field>
            <multiselect
              v-model="selectExportVsPointsExceptions"
              tag-placeholder=""
              placeholder="Select exceptions"             
              :options="exportVsPointsExceptions"
              :multiple="true"
              :taggable="true"
              selectLabel="Add"
              deselectLabel="Remove"
            >
            </multiselect>
          </b-field>        
        </b-field>

        <b-field grouped class="mb-3">
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">Export vs Points (in Points) from</p>
          </div>
          <b-field>
            <b-input v-model="filter.exportVsPointsNumberFrom" type="number"></b-input>
          </b-field>
          <div class="mr-3">
            <p class="subtitle is-6 mt-3">to</p>
          </div>
          <b-field>
            <b-input v-model="filter.exportVsPointsNumberTo" type="number"></b-input>
          </b-field>  
        </b-field>
      </div>

      <b-field grouped>
        <p class="control">
          <b-button
            label="Search"
            type="is-link"
            class="mr-3"
            icon-left="magnify"
            @click="getCustomerCount"
            :loading="isLoading"
          />
          <b-button
            label="Reset"
            type="is-light"
            class="mr-3"
            icon-left="reload"
            @click="resetFilter"
          />
          <b-button
            label="Download result"
            class="mr-3"
            type="is-primary"
            @click="downloadCustomerList(false)"
            :disabled="isDisableDownload"
            :loading="isLoadingDownload"
          />
          <b-button            
            class="mr-3"
            type="is-primary"
            @click="downloadCustomerList(true)"
            :disabled="isDisableDownload"
            :loading="isLoadingDownloadAndRemove"
          >
          <p class="is-align-content-space-evenly	"></p>
            <span>Download and Remove 'Tagged Campaign'</span>
          </b-button>
        </p>
       <!-- <b-field
          class="control"
          v-show="isShowCampaign"
        >
          <b-select
            placeholder="Select campaign"
            v-model="filter.campaignID"
            clearable
          >
            <option
              v-for="option in adminCampaigns"
              :value="option.id"
              :key="option.id"
            >
              {{ option.name }}
            </option>
          </b-select>
          <b-button
            label="Confirm"
            type="is-primary"
            @click="assignCampaignToCustomers"
            :disabled="!filter.campaignID"
            :loading="isConfirmingCampaign"
          />          
        </b-field> -->
      </b-field>
        <b-modal v-model="isImageModalActive"  :width="`100%`" scroll="keep">
          <figure  class="image is-3by1 is-fullwidth">
            <img src="../assets/lead_management_report.png">
          </figure>
      </b-modal>
    </section>
  </div>
</template>

<script>
import moment from "moment";
import Multiselect from "vue-multiselect";
import { saveAs } from 'file-saver';
import { getAdminScores, getAdminCampaigns } from "@/api/importData";
import { getCustomerCount, assignCampaignToCustomers, downloadCustomersBySP, removeAssignedCampaign, countCustomersOfTaggedCampagign } from "@/api/exportData";
export default {
  name: "ExportData",
  components: { Multiselect},
  created() {
    this.getAdminScoreList();
    this.getAdminCampaignList();
  },
  data() {
    return {
      adminScores: [],
      adminCampaigns: [],
      customizedAdminCampaigns: [],
      occurredCategories:[],
      resultsCategories:[],
      exportVsPointsExceptions:["No Occurance","No Export"],
      selectOccurredCategories:[],
      selectResultsCategories:[],
      selectExportVsPointsExceptions:[],
      selectLast3CampaignsUsed:[],
      filter: {
        dateFirstAddedFrom: null,
        dateFirstAddedTo: null,

        totalTimesExportedFrom: null,
        totalTimesExportedTo: null,
        
        dateLastExportedFrom: null,
        dateLastExportedTo: null,

        assignedCampaignID:null,
        last3CampaignsUsed:null,

        dateLastOccurredFrom:null,
        dateLastOccurredTo:null,

        occurredCategories:null,
        
        totalOccurancePointsFrom:null,
        totalOccurancePointsTo:null,

        resultsCategories:null,

        totalResultsPointsFrom:null,
        totalResultsPointsTo:null,

        totalPointsFrom:null,
        totalPointsTo:null,

        exportVsPointsPercentageFrom:null,
        exportVsPointsPercentageTo:null,

        exportVsPointsExceptions:null,

        exportVsPointsNumberFrom:null,
        exportVsPointsNumberTo:null,

        sortBy:null,
        sortDirection:null,
        sortingValue:null,
        exportTop:null,

        campaignID: null,
        totalCount: 0,

        isRemoveTaggedCampaign:false
      },
      defaultFilter: {
        dateFirstAddedFrom: null,
        dateFirstAddedTo: null,

        totalTimesExportedFrom: null,
        totalTimesExportedTo: null,
        
        dateLastExportedFrom: null,
        dateLastExportedTo: null,

        assignedCampaignID:null,
        last3CampaignsUsed:null,

        dateLastOccurredFrom:null,
        dateLastOccurredTo:null,

        occurredCategories:null,
        
        totalOccurancePointsFrom:null,
        totalOccurancePointsTo:null,

        resultsCategories:null,

        totalResultsPointsFrom:null,
        totalResultsPointsTo:null,

        totalPointsFrom:null,
        totalPointsTo:null,

        exportVsPointsPercentageFrom:null,
        exportVsPointsPercentageTo:null,

        exportVsPointsExceptions:null,

        exportVsPointsNumberFrom:null,
        exportVsPointsNumberTo:null,

        sortBy:null,
        sortDirection:null,
        sortingValue:null,
        exportTop:null,

        campaignID: null,
        totalCount: 0,

        isRemoveTaggedCampaign:false
      },
      dateFirstAddedFrom: null,
      dateFirstAddedTo: null,   
      dateLastExportedFrom: null,
      dateLastExportedTo: null,
      dateLastOccurredFrom: null,
      dateLastOccurredTo: null,         
      customerList: [],
      totalCount: 0,
      isShowResult: false,
      isLoading: false,
      isLoadingDownload: false,
      isLoadingDownloadAndRemove: false,
      isShowCampaign: false,
      isConfirmingCampaign: false,
      isImageModalActive:false,
      selectAssignedCampaign:null,
      loadingRemoveAssignedCampaign:false,
      customersOfTaggedCampagignCount:null,
      isLoadingCustomersOfTaggedCampagignCount:false,
      isValidFilter:true,
      isShowDateFirstAddedFrom:false,
      isShowOccurrence:false,
      isShowResults:false,
      isShowAnalysis:false,
    };
  },
  computed: {
    isDisableDownload() {
      return this.totalCount == 0;
    },
    formattedTotalCount(){
      if(!this.totalCount) return '0';
      return this.totalCount.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    },
    hasTaggedCampagin(){
      return this.selectAssignedCampaign?true:false;
    },
    hasDateLastExport(){
      return (this.dateLastExportedFrom && this.dateLastExportedTo)?true:false;
    },
    hasTotalNumber(){
      return this.filter.exportTop || this.filter.exportTop===0;
    }
  },
  watch:{
    selectAssignedCampaign(value){
      if(value && value.id)
        this.countCustomersOfTaggedCampagign(value.id);
      else
        this.customersOfTaggedCampagignCount=null;
    }
  },
  methods: {
    resetFilter() {
      this.filter = { ...this.defaultFilter };
      this.totalCount=0;
      this.customerList = [];
      this.dateFirstAddedFrom = null;
      this.dateFirstAddedTo = null;
      this.dateLastExportedFrom = null;
      this.dateLastExportedTo = null;
      this.isShowResult = false;
      this.isShowCampaign = false;
      this.selectLast3CampaignsUsed=[];
      this.selectOccurredCategories=[];
      this.selectResultsCategories=[];
      this.selectExportVsPointsExceptions=[];
      this.selectAssignedCampaign=null;
    },
    getAdminScoreList() {
      //this.isLoading = true;
      getAdminScores()
        .then((response) => {
          if (response.status == 200) {
            this.adminScores = response.data;
            this.occurredCategories= this.adminScores.filter(p=>p.scoreCategory=='Occurance');
            this.resultsCategories=this.adminScores.filter(p=>p.scoreCategory=='Results');
            //console.log(this.adminScores);
          } else if (response.status == 401) {
            this.$router.push({ name: "login" });
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          //this.isLoading = false;
        });
    },
    getAdminCampaignList() {
      //this.isLoading = true;
      getAdminCampaigns()
        .then((response) => {
          if (response.status == 200) {
            this.adminCampaigns = response.data;
            this.customizedAdminCampaigns= [{name:"None", id:0},...this.adminCampaigns]
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          //this.isLoading = false;
        });
    },
    processFilter(){
      this.isValidFilter=false;
      if(!this.dateLastExportedFrom || !this.dateLastExportedTo){
        console.log(this.dateLastExportedFrom);
        console.log(this.dateLastExportedTo);
        this.$buefy.snackbar.open({
        message: "Missing Date last export!",
        queue: false,
        type: 'is-danger'
        });
        return false;   
      }
      if(!this.selectAssignedCampaign ){
        this.$buefy.snackbar.open({
        message: "Missing Tagged Campaign!",
        queue: false,
        type: 'is-danger'
        });
        return false;   
      }
      if(!this.filter.exportTop){
        this.$buefy.snackbar.open({
        message: "Missing Total Number!",
        queue: false,
        type: 'is-danger'
        });
        return false;   
      }
      const outputFormat = "YYYY-MM-DD";
      this.filter.dateFirstAddedFrom =this.dateFirstAddedFrom? moment(this.dateFirstAddedFrom).format(outputFormat):null;
      this.filter.dateFirstAddedTo =this.dateFirstAddedTo? moment(this.dateFirstAddedTo).format(outputFormat):null;

      this.filter.dateLastExportedFrom =this.dateLastExportedFrom? moment(this.dateLastExportedFrom).format(outputFormat):null;
      this.filter.dateLastExportedTo =this.dateLastExportedTo? moment(this.dateLastExportedTo).format(outputFormat):null;

      this.filter.dateLastOccurredFrom =this.dateLastOccurredFrom? moment(this.dateLastOccurredFrom).format(outputFormat):null;
      this.filter.dateLastOccurredTo =this.dateLastOccurredTo? moment(this.dateLastOccurredTo).format(outputFormat):null;      

      this.filter.last3CampaignsUsed =this.selectLast3CampaignsUsed.length>0?
        this.selectLast3CampaignsUsed.map((p) => p.campaignID).join() : null;

      this.filter.occurredCategories = this.selectOccurredCategories.length>0?
        this.selectOccurredCategories.map((p) => p.scoreID).join(): null;
      
      this.filter.resultsCategories =this.selectResultsCategories.length>0?
        this.selectResultsCategories.map((p) => p.scoreID).join(): null;
      
      this.filter.exportVsPointsExceptions =this.selectExportVsPointsExceptions.length>0?
        this.selectExportVsPointsExceptions.join(): null;
      
      if(this.filter.sortingValue){
        const sortingValue = this.filter.sortingValue.split(" ");
        this.filter.sortBy=sortingValue[0];
        this.filter.sortDirection=sortingValue[1];
      }else{
        this.filter.sortBy=null;
        this.filter.sortDirection=null;
      }

      if(!this.filter.exportTop ||isNaN(this.filter.exportTop))
        this.filter.exportTop=null;

      this.filter.assignedCampaignID = this.selectAssignedCampaign? this.selectAssignedCampaign.id:null;
      return true;
    },
    getCustomerCount() {
      var result = this.processFilter();
      if(!result) return;
      this.isLoading = true;
      getCustomerCount(this.filter)
        .then((response) => {
          if (response.status == 200 && response.data) {
            const data=  response.data;
            this.filter.totalCount=  this.totalCount= data.totalCount;
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          this.isShowResult = true;
          this.isLoading = false;
          this.isShowCampaign = false;
        });
    },
    downloadCustomerList(isRemoveTaggedCampaign=false) {
      var result = this.processFilter();
      if(!result) return;
      if(isRemoveTaggedCampaign){
        this.isLoadingDownloadAndRemove = true;
        this.filter.isRemoveTaggedCampaign = true;
      }  
      else{
        this.isLoadingDownload=true;
        this.filter.isRemoveTaggedCampaign= false;
      }      
      downloadCustomersBySP(this.filter)
        .then((response) => {
          if (response.status == 200 && response.data) {
            const data =  response.data;
            if(data.result){
              const result=data.result;
              for (let i = 0; i < result.length; i++) {   
                let filename = result[i].substring(result[i].lastIndexOf('/')+1);
                let fileUrl= result[i];
                setTimeout(function() {          
                  console.log(filename);
                  saveAs(fileUrl, filename); },200);
              }
            } 
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          this.isLoadingDownload=false;
          this.isLoadingDownloadAndRemove=false;
          this.filter.isRemoveTaggedCampaign= false;
        });
    },
    downloadCustomerExcel() {
      console.log("downloadCustomerExcel");
      if (this.customerList.length > 0) {
        let mobileList = this.customerList.map((p) => ({
          CustomerMobileNo: p,
        }));
        this.exportExcelData(mobileList, "CustomerMobileNoList", 60, false);
      }
    },
    assignCampaignToCustomers() {
      this.isConfirmingCampaign = true;
      this.filter.signalRConnectionId= this.$store.state.signalRConnectionId;
      assignCampaignToCustomers(this.filter)
        .then((response) => {
          if (response.status == 200) {
            var data = response.data;
            if(!data.shouldSendEmail){
                 this.$buefy.snackbar.open({
                  message: `Assign campaign successfully!`,
                  queue: false,
                });
              }else{
                this.$buefy.snackbar.open({
                  message: `Assign campaign successfully!\nSystem will send email to inform when the new export data is ready`,
                  queue: false,
                  duration: 6000
                });
              }
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          // this.isShowResult = true;
          // this.isLoading = false;
          this.filter.campaignID = null;
          this.isConfirmingCampaign = false;
        });
    },
    removeAssignedCampaign(){
      if(!this.selectAssignedCampaign.campaignID) 
        return;
      this.loadingRemoveAssignedCampaign=true;
      removeAssignedCampaign(this.selectAssignedCampaign.campaignID)
        .then((response) => {
          if (response.status == 200) {
            this.$buefy.snackbar.open({
                message: `Remove assigned campaign successfully!`,
                queue: false,
              });
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          this.selectAssignedCampaign = null;
          this.loadingRemoveAssignedCampaign=false;
        });
    },
    removeAssignedCampaign(){
      if(!this.selectAssignedCampaign.campaignID) 
        return;
      this.loadingRemoveAssignedCampaign=true;
      removeAssignedCampaign(this.selectAssignedCampaign.campaignID)
        .then((response) => {
          if (response.status == 200) {
            this.$buefy.snackbar.open({
                message: `Remove assigned campaign successfully!`,
                queue: false,
              });
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          this.selectAssignedCampaign = null;
          this.loadingRemoveAssignedCampaign=false;
        });
    },
    countCustomersOfTaggedCampagign(id) {
      this.isLoadingCustomersOfTaggedCampagignCount = true;
      countCustomersOfTaggedCampagign(id)
        .then((response) => {
          if (response.status == 200 &&Number.isInteger(response.data)) 
            this.customersOfTaggedCampagignCount= response.data;
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          this.isLoadingCustomersOfTaggedCampagignCount = false;
        });   
    },
    // testFocus(){
    //   //this.$refs.multiselectSelectAssignedCampaign.$el.focus();
    //   this.$refs.dateLastExportedFrom.$el.focus();
    //   this.$refs.dateLastExportedTo.$el.focus();
    //   this.$refs.exportTop.$el.focus();
    //   this.$refs.dateLastExportedFrom.$el.style.backgroundColor = "#FDFF47"; 
    // },
  },
};
</script>
<style src="vue-multiselect/dist/vue-multiselect.min.css"></style>
